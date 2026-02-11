using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderPublisher.Helpers
{
    public static class SerializeHelper
    {
        public static byte[] ToBody<T>(T poco)
        {
            var json = JsonSerializer.Serialize(poco);
            return Encoding.UTF8.GetBytes(json);
        }

    }
}
