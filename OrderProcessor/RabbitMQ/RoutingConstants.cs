using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessor.RabbitMQ
{
    public static class RoutingConstants
    {
        public const int DelayOrderCreatedRetryMiliseconds = 3_000;

        public static class Exchanges
        {
            public const string OrderEventsDlx = "dlx.order.events";
            public const string OrdersEvents = "orders.events";
            public const string OrdersCreatedRetry = "orders.created.retry";


        }

        public static class Queues
        {
            public const string OrderCreatedDlq = "order.created.dlq";
            public const string OrderCreatedWorker = "order.created.worker";
        }

        public static class RoutingKeys
        {
            public const string OrdersDead = "orders.dead";
            public const string OrdersCreatedRetry = "orders.created.retry.3s";
        }

    }
}
