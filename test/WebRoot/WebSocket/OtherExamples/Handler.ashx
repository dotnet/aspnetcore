<%@ WebHandler Language="C#" Class="Handler" %>

using System;
using System.Web;
using System.Net.WebSockets;
using System.Web.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

public class Handler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.AcceptWebSocketRequest(async wsContext =>
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024*1024]);
            WebSocket socket = wsContext.WebSocket;

            // set up the loop
            while (true)
            {
                var input = await socket.ReceiveAsync(buffer, CancellationToken.None);     
                Thread.Sleep(100);
                
                if (input.CloseStatus != null)
                {
                    await socket.CloseAsync(input.CloseStatus.Value, input.CloseStatusDescription, CancellationToken.None);
                    break;
                }
                else
                {
                    var outputBuffer = new ArraySegment<byte>(buffer.Array, 0, input.Count);
                    await socket.SendAsync(outputBuffer, input.MessageType, input.EndOfMessage, CancellationToken.None);
                }
            }
        });        
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}