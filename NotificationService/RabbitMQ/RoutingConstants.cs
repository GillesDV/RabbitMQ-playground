using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.RabbitMQ
{
    public static class RoutingConstants
    {

        public static class Exchanges
        {
            public const string OrdersEvents = "orders.events";

        }

        public static class Queues
        {
            public const string OrderCreatedListener = "order.created.listener";
        }

        public static class RoutingKeys
        {
            public const string OrderCreated = "order.created";
        }

    }
}
