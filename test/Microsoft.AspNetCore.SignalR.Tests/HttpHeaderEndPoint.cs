using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Http.Features;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HttpHeaderEndPoint : EndPoint
    {
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            try
            {
                var headers = connection.Features.Get<IHttpContextFeature>().HttpContext.Request.Headers;

                var headerName = Encoding.UTF8.GetString(buffer.ToArray());
                var headerValues = headers.FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase)).Value.ToArray();
                
                var data = Encoding.UTF8.GetBytes(string.Join(",", headerValues));

                await connection.Transport.Output.WriteAsync(data);
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
            }
        }
    }
}