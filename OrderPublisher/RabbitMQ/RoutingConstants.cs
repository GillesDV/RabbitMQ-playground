using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPublisher.RabbitMQ
{
    public static class RoutingConstants
    {
        public const string ExchangeName = "order_exchange";
        public const string RoutingKey = "order.created";
    }
}
