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
    const int BUFFER = 1024*5;
    bool StopFalg = false;
    
    public void ProcessRequest(HttpContext context)
    {
        if (!context.IsWebSocketRequest)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;
            Response.Write("Simple Http");
            return;
        }
        
        context.AcceptWebSocketRequest(async wsContext =>
        {
            
            // set up the loop
            WebSocket socket = wsContext.WebSocket;
            WebSocketMessageType responseType = WebSocketMessageType.Text;
            int returnSize = 1024*1024; 
            
            Thread.Sleep(500);
            
            Task.Run(() =>
            {
                Recieve(socket);
            });

            Thread.Sleep(500);

            while (socket.State == WebSocketState.Open)
            {
                int bytesLeft = returnSize;
                var tempString = string.Empty;

                while (bytesLeft > BUFFER)
                {
                    tempString = RandomString(BUFFER);
                    bytesLeft -= BUFFER;
                    await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(tempString)), responseType, false, CancellationToken.None);
                    Thread.Sleep(500);
                }

                if (bytesLeft <= BUFFER && bytesLeft >= 0)
                {
                    tempString = RandomString(bytesLeft);
                    await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(tempString)), responseType, true, CancellationToken.None);
                    Thread.Sleep(500);
                }

                if (StopFalg) break;
            }
        });
    }

    async Task Recieve(WebSocket webSocket)
    {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult input = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (input.CloseStatus == WebSocketCloseStatus.NormalClosure)
                {
                    StopFalg = true;
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break; 
                }
            }
    }


    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    public string RandomString(int size)
    {
        if (size < 1)
            return string.Empty;
        
        Random random = new Random((int)DateTime.Now.Ticks);
        StringBuilder builder = new StringBuilder();
        char ch;
        for (int i = 0; i < size; i++)
        {
            ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
            builder.Append(ch);
        }

        return builder.ToString();        
    }

}