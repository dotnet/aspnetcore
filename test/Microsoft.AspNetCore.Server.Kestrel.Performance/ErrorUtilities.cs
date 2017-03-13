using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public static class ErrorUtilities
    {
        public static void ThrowInvalidRequestLine()
        {
            throw new InvalidOperationException("Invalid request line");
        }

        public static void ThrowInvalidRequestHeaders()
        {
            throw new InvalidOperationException("Invalid request headers");
        }
    }
}
