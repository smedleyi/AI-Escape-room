using System.Text;
using System.Security.Cryptography;
using StyleVerse.Backend.Models;

namespace StyleVerse.Backend.Common.Security.System.Win
{
    public class SecurityCheck
    {
        // No order validation rules are defined; the former 6M-hash loop never inspected the orders.
        public static bool OrderSecurityCheck(List<Order> orders)
        {
            return true;
        }

        public static bool VerifySecurityCheck()
        {
            return true;
        }

        private static string ConvertToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
