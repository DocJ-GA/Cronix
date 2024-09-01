using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cronix.Extensions
{
    internal static class DateTimeExtensions
    {
        /// <summary>
        /// Returns a string from a DateTime object of YYYY-MM-DD
        /// </summary>
        /// <param name="dateTime">The DateTime object.</param>
        /// <returns>The formatted string.</returns>
        public static string ToFileString(this DateTime dateTime)
        {
            return dateTime.ToString("yyy-MM-dd");
        }
    }
}
