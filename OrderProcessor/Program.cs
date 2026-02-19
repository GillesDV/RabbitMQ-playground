using Domain.Models;
using OrderProcessor.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections;
using System.Text;
using System.Text.Json;

const int MaxAttempts = 4;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Normally this would be done in a setup script or IaC, but for a small demo it's done here, once.
await SetupExchangeAndQueues(channel);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (sender, ea) =>
{
    var json = Encoding.UTF8.GetString(ea.Body.Span);
    var poco = JsonSerializer.Deserialize<OrderCreatedEvent>(json)!;

    if (poco.Quantity <= 0)
    {
        Console.WriteLine($"Received an order with negative quantity. Sent to DLQ!");

        // channel.BasicCancel would unsubscribe from the queue. We don't want that.
        await SendMessageToDlq(ea.DeliveryTag, channel, poco);

        return;
    }

    if (poco.Quantity % 2 == 1)
    {
        if (poco.AttemptInQueue > MaxAttempts)
        {
            Console.WriteLine($"Received an odd quantity for {poco.AttemptInQueue} attempts. Off to DLQ!");

            await SendMessageToDlq(ea.DeliveryTag, channel, poco);

            return;
        }
        else
        {
            Console.WriteLine($"Received an odd quantity. Retrying. Currently number: {poco.AttemptInQueue}");

            await SendMessageToRetryExchange(ea.DeliveryTag, channel, poco);

            return;
        }
    }

    Console.WriteLine($"Received & acknowledged: {poco}");

    // Acknowledge the message 
    await channel.BasicAckAsync(
        deliveryTag: ea.DeliveryTag,
        multiple: false
    );

};

await channel.BasicConsumeAsync(
    queue: RoutingConstants.Queues.OrderCreatedWorker,
    autoAck: false, // do not double acknowledge. That causes errors
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();



static async Task SetupExchangeAndQueues(IChannel channel)
{
    // Create DLQ exchange & queue

    await channel.ExchangeDeclareAsync(RoutingConstants.Exchanges.OrderEventsDlx, ExchangeType.Direct, durable: true);
    await channel.QueueDeclareAsync(RoutingConstants.Queues.OrderCreatedDlq, durable: true, exclusive: false, autoDelete: false);
    await channel.QueueBindAsync(RoutingConstants.Queues.OrderCreatedDlq, RoutingConstants.Exchanges.OrderEventsDlx,
        routingKey: RoutingConstants.RoutingKeys.OrdersDead, arguments: null);

    // Create main exchange & queue, with DLQ args
    var dlqArgs = new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = RoutingConstants.Exchanges.OrdersCreatedRetry,
        ["x-dead-letter-routing-key"] = RoutingConstants.RoutingKeys.OrdersCreatedRetry
    };

    await channel.ExchangeDeclareAsync(RoutingConstants.Exchanges.OrdersEvents, ExchangeType.Fanout, durable: true);
    await channel.QueueDeclareAsync(RoutingConstants.Queues.OrderCreatedWorker, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);

    await channel.QueueBindAsync(
        queue: RoutingConstants.Queues.OrderCreatedWorker,
        exchange: RoutingConstants.Exchanges.OrdersEvents,
        routingKey: "");

    // Add a retry queue with TTL and DLX back to main exchange
    // Retry exchange + retry queue (delay) -> dead-letter back to the main fanout exchange
    await channel.ExchangeDeclareAsync(RoutingConstants.Exchanges.OrdersCreatedRetry, ExchangeType.Direct, durable: true);

    var retryArgs = new Dictionary<string, object>
    {
        ["x-message-ttl"] = RoutingConstants.DelayOrderCreatedRetryMiliseconds, 
        ["x-dead-letter-exchange"] = RoutingConstants.Exchanges.OrdersEvents,
        ["x-dead-letter-routing-key"] = "" // fanout ignores it, but set it explicitly anyway
    };

    await channel.QueueDeclareAsync(
        queue: RoutingConstants.RoutingKeys.OrdersCreatedRetry,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: retryArgs);

    await channel.QueueBindAsync(
        queue: RoutingConstants.RoutingKeys.OrdersCreatedRetry,
        exchange: RoutingConstants.Exchanges.OrdersCreatedRetry,
        routingKey: RoutingConstants.RoutingKeys.OrdersCreatedRetry);

}

async Task SendMessageToDlq(ulong deliveryTag, IChannel channel, OrderCreatedEvent poco)
{
    await PublishAsync(channel, RoutingConstants.Exchanges.OrderEventsDlx, RoutingConstants.RoutingKeys.OrdersDead, poco);

    await channel.BasicAckAsync(deliveryTag, multiple: false);
}

async Task SendMessageToRetryExchange(ulong deliveryTag, IChannel channel, OrderCreatedEvent poco)
{
    // Publish to retry exchange/route (puts it in retry queue with TTL)
    await PublishAsync(channel, RoutingConstants.Exchanges.OrdersCreatedRetry, RoutingConstants.RoutingKeys.OrdersCreatedRetry, poco);

    // Normally Nack would be sufficient thanks to the DLX config, but since we already send a POCO with a bumped retry on the queue, this would send 2 messages on that queue (one with original number and one with bumped number) 
    await channel.BasicAckAsync(deliveryTag, multiple: false);
}

static async Task PublishAsync(
    IChannel channel,
    string exchange,
    string routingKey,
    OrderCreatedEvent poco)
{

    poco.AttemptInQueue++;

    var json = JsonSerializer.Serialize(poco);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent // 2 = persistent
    };

    await channel.BasicPublishAsync(
        exchange: exchange,
        routingKey: routingKey,
        mandatory: false,
        basicProperties: props,
        body: body);
}