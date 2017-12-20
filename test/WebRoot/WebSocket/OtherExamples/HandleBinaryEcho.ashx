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
    const int BUFFER = 1000 * 1000;
    
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

            ////determine return type
            List<string> query = wsContext.SecWebSocketProtocols.ToList();

            WebSocketMessageType responseType = WebSocketMessageType.Text;
            if (query[0].Split('-')[0] == WebSocketMessageType.Binary.ToString())
            {
                responseType = WebSocketMessageType.Binary;
            }
            WebSocketMessageType requestType = WebSocketMessageType.Text;
            if (query[0].Split('-')[1] == WebSocketMessageType.Binary.ToString())
            {
                requestType = WebSocketMessageType.Binary;
            }
            int returnSize = Int32.Parse(query[0].Split('-')[2]);
            int requestSize = Int32.Parse(query[0].Split('-')[3]);
            bool canSend = Boolean.Parse(query[0].Split('-')[4]);
            bool canReceive = Boolean.Parse(query[0].Split('-')[5]);





            if (canSend && !canReceive)
            {
                await Send(socket, responseType, returnSize);
            }

            else if (canReceive && !canSend)
            {
                await Recieve(socket, requestType);
            }

            else if (canReceive && canSend)
            {

                Task.Run(() =>
                {
                    Recieve(socket, requestType);
                });

                while (socket.State == WebSocketState.Open)
                {
                    int bytesLeft = returnSize;
                    var tempString = string.Empty;

                    while (bytesLeft > BUFFER)
                    {
                        tempString = RandomString(BUFFER);
                        bytesLeft -= BUFFER;
                        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(tempString)), responseType, false, CancellationToken.None);
                        Thread.Sleep(200);
                    }

                    if (bytesLeft <= BUFFER && bytesLeft >= 0)
                    {
                        tempString = RandomString(bytesLeft);
                        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(tempString)), responseType, true, CancellationToken.None);
                        Thread.Sleep(500);
                    }
                }
            }
            else
            {

                Task.Run(() =>
                {
                    Recieve(socket, requestType);
                });
            }
        });
    }

    async Task Recieve(WebSocket webSocket, WebSocketMessageType messageType)
    {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult input = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (input.CloseStatus == WebSocketCloseStatus.NormalClosure)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break; 
                }
            }
    }

    async Task Send(WebSocket socket, WebSocketMessageType responseType, int returnSize)
    {
        while (socket.State == WebSocketState.Open)
        {
            int bytesLeft = returnSize;

            while (bytesLeft > BUFFER)
            {
                bytesLeft -= BUFFER;
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(RandomString(BUFFER))), responseType, false, CancellationToken.None);
                Thread.Sleep(200);
            }

            if (bytesLeft <= BUFFER && bytesLeft >= 0)
            {
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(RandomString(bytesLeft))), responseType, true, CancellationToken.None);
                Thread.Sleep(500);
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