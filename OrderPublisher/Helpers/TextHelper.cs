using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPublisher.Helpers
{
    public static class TextHelper
    {
        public static bool IsValidNumber(this string input)
        {
            return int.TryParse(input, out var formattedInput); // don't check for negative number so we can put something in DLQ later
        }
    }
}
