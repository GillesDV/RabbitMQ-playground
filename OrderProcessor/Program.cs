using OrderProcessor.Models;
using OrderProcessor.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(
      queue: RoutingConstants.RoutingKey,
      durable: false,
      exclusive: false,
      autoDelete: false,
      arguments: null
  );

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (sender, ea) =>
{
    var json = Encoding.UTF8.GetString(ea.Body.Span);
    var poco = JsonSerializer.Deserialize<OrderCreated>(json)!;

    Console.WriteLine(
        $"Received: {poco}"
    );

    // Acknowledge the message 
    await channel.BasicAckAsync(
        deliveryTag: ea.DeliveryTag,
        multiple: false
    );
};

await channel.BasicConsumeAsync(
    queue: RoutingConstants.RoutingKey,
    autoAck: false, // do not double acknowledge. That causes errors
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();