using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPublisher.RabbitMQ
{
    public static class RoutingConstants
    {
        public static class RoutingKeys
        {
            public const string OrderCreated = "order.created";
        }
    }
}
