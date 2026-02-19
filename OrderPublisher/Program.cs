using Domain.Models;
using OrderPublisher.Helpers;
using OrderPublisher.RabbitMQ;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();


//infinite loop, will run until the user types "exit"
for (; ; )
{
    Console.WriteLine("How many products did they order? Negative will go straight to DLQ. Odd quantities will be test the retry-mechanism");

    string? input = Console.ReadLine();

    // Exit condition
    if (input?.Equals("exit", StringComparison.InvariantCultureIgnoreCase) ?? false)
    {
        Console.WriteLine("Exiting input loop...");
        break;
    }

    if (string.IsNullOrWhiteSpace(input) ||
        !input.IsValidNumber())
    {
        Console.WriteLine("Invalid input, try again.");
        continue;
    }

    var newOrder = new OrderCreatedEvent(Guid.NewGuid(), "Sample Product", int.Parse(input), DateTimeOffset.UtcNow);

    var json = JsonSerializer.Serialize(newOrder);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        Type = nameof(OrderCreatedEvent)
    };

    await channel.ExchangeDeclareAsync(
    exchange: RoutingConstants.Exchanges.OrdersEvents,
    type: ExchangeType.Fanout,
    durable: true);

    await channel.BasicPublishAsync(
        exchange: RoutingConstants.Exchanges.OrdersEvents,
    routingKey: string.Empty, // ignored for Fanout exchange
    mandatory: false,
    basicProperties: props,
    body: body
);

    // Process input
    Console.WriteLine($" [x] New product sent: {newOrder}");
}


Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

