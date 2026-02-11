using OrderPublisher.Helpers;
using OrderPublisher.Models;
using OrderPublisher.RabbitMQ;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: RoutingConstants.RoutingKey, durable: false, exclusive: false, autoDelete: false,
    arguments: null);


//infinite loop, will run until the user types "exit"
for (; ; )
{
    Console.WriteLine("How many products did they order? (or type 'exit' to close program) ");

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

    var newOrder = new OrderCreated(Guid.NewGuid(), "Sample Product", int.Parse(input), DateTimeOffset.UtcNow);

    var json = JsonSerializer.Serialize(newOrder);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        Type = nameof(OrderCreated)
    };

    await channel.BasicPublishAsync(
        exchange: string.Empty,
    routingKey: RoutingConstants.RoutingKey,
    mandatory: false,
    basicProperties: props,
    body: body
);

    // Process input
    Console.WriteLine($" [x] New product sent: {newOrder}");
}


Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

