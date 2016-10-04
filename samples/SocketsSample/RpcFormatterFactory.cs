using System;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class RpcFormatterFactory : IFormatterFactory
    {
        public IFormatter CreateFormatter(Format format, string formatType)
        {
            if (format == Format.Text)
            {
                switch(formatType)
                {
                    case "json":
                        return new RpcJSonFormatter();
                    case "line":
                        return new RpcTextFormatter();
                }
            }

            throw new InvalidOperationException($"No formatter for format '{format}' and formatType 'formatType'.");
        }
    }
}
