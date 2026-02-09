using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPublisher.Models
{
    public class OrderCreated
    {
        public Guid OrderId { get; set; }
        public string ProductName { get; set; }
        public int Quality { get; set; }
        public DateTimeOffset CreatedAt { get; set; }


    }
}
