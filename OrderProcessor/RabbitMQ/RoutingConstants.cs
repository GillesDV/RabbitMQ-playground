using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessor.RabbitMQ
{
    public static class RoutingConstants
    {

        public static class Exchanges
        {
            public const string OrderDeadLetter = "dlx.order";
        }

        public static class Queues
        {
            public const string OrderCreatedDlq = "order.created.dlq";
        }

        public static class RoutingKeys
        {
            public const string OrderCreated = "order.created";
        }

    }
}
