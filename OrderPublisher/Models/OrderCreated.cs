using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPublisher.Models
{
    public record OrderCreated
    {
        public Guid OrderId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public OrderCreated(Guid orderId, string productName, int quantity, DateTimeOffset createdAt)
        {
            OrderId = orderId;
            ProductName = productName;
            Quantity = quantity;
            CreatedAt = createdAt;
        }

        public override string ToString()
        {
            return $"OrderCreated: OrderId={OrderId}, ProductName={ProductName}, Quantity={Quantity}, CreatedAt={CreatedAt}";
        }
    }
}
