using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessor.RabbitMQ
{
    public static class ExchangeAndQueuesSetup
    {

        public static async Task Setup(IChannel channel)
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
    }
}
