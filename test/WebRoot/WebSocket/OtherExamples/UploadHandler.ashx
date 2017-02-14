<%@ WebHandler Language="C#" Class="Handler" %>

using System;
using System.Web;
using System.Net.WebSockets;
using System.Web.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Linq;

public class Handler : IHttpHandler
{    
    public void ProcessRequest(HttpContext context)
    {
        
        context.AcceptWebSocketRequest(async wsContext =>
        {
            
            // set up the loop
            WebSocket socket = wsContext.WebSocket;
            
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            
            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult input = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (input.CloseStatus == WebSocketCloseStatus.NormalClosure)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
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