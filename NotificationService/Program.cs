using NotificationService.Models;
using NotificationService.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
    var poco = JsonSerializer.Deserialize<OrderCreated>(json)!;

    Console.WriteLine($"Received & acknowledged. I'm gonna send an e-mail for this quantity received: {poco.Quantity}");

};

await channel.BasicConsumeAsync(
    queue: RoutingConstants.Queues.OrderCreatedListener,
    autoAck: true, 
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

static async Task SetupExchangeAndQueues(IChannel channel)
{
    await channel.ExchangeDeclareAsync(RoutingConstants.Exchanges.OrdersEvents, ExchangeType.Fanout, durable: false);

    await channel.QueueDeclareAsync(RoutingConstants.Queues.OrderCreatedListener, durable: false, exclusive: false, autoDelete: false);

    await channel.QueueBindAsync(
        queue: RoutingConstants.Queues.OrderCreatedListener,
        exchange: RoutingConstants.Exchanges.OrdersEvents,
        routingKey: "");

}