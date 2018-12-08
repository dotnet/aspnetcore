using System.Text;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    internal static class EncodingHelper
    {
        internal static string ToUTF8(this string text)
        {
            var bytes = Encoding.Default.GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
