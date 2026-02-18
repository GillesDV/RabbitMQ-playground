using Domain.Models;
using OrderProcessor.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections;
using System.Text;
using System.Text.Json;

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
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: false // don't requeue, send to DLQ
        );
        // channel.BasicCancel would unsubscribe from the queue. We don't want that.

        return;
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
        routingKey: RoutingConstants.RoutingKeys.OrdersDead);

    var dlqArgs = new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = RoutingConstants.Exchanges.OrderEventsDlx,
        ["x-dead-letter-routing-key"] = RoutingConstants.RoutingKeys.OrdersDead
    };
        
    // Create main exchange & queue, with DLQ args

    await channel.ExchangeDeclareAsync(RoutingConstants.Exchanges.OrdersEvents, ExchangeType.Fanout, durable: true);
    await channel.QueueDeclareAsync(RoutingConstants.Queues.OrderCreatedWorker, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);

    await channel.QueueBindAsync(
        queue: RoutingConstants.Queues.OrderCreatedWorker,
        exchange: RoutingConstants.Exchanges.OrdersEvents,
        routingKey: "");
}