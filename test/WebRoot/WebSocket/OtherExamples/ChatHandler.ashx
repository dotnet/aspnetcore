<%@ WebHandler Language="C#" Class="ChatStartHandler" %>

using System;
using System.Web;
using System.Net.WebSockets;
using System.Web.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class ChatStartHandler : IHttpHandler
{

    static int clientCount=0;
    
    public void ProcessRequest(HttpContext context)
    {
       
        context.AcceptWebSocketRequest(async wsContext =>
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocket socket = wsContext.WebSocket;
            ChatList.ActiveChatSessions.TryAdd(clientCount++, socket);

            // set up the loop
            while (true)
            {
                var input = await socket.ReceiveAsync(buffer, CancellationToken.None);              
                
                if (input.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
                else
                {

                    foreach (KeyValuePair<int, WebSocket> kvp in ChatList.ActiveChatSessions)
                    {
                        WebSocket ws = kvp.Value;
                        if (ws.State == WebSocketState.Open)
                        {
                            var outputBuffer = new ArraySegment<byte>(buffer.Array, 0, input.Count);
                            await ws.SendAsync(outputBuffer, input.MessageType, input.EndOfMessage, CancellationToken.None);
                        }                        
                    }
                }
            }
        });
        //}, new System.Web.WebSockets.AspNetWebSocketOptions { Subprotocol = "ECHO" });
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}

public static class ChatList
{
    public static ConcurrentDictionary<int, WebSocket> ActiveChatSessions = new ConcurrentDictionary<int, WebSocket>();
}
