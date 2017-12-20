<%@ WebHandler Language="C#" Class="GetWebSocketConnectionCount" %>

using System;
using System.Web;

public class GetWebSocketConnectionCount : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(@"System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        object mc = assembly.CreateInstance("System.Web.WebSockets.AspNetWebSocketManager");
        Type t = mc.GetType();
        System.Reflection.BindingFlags bf =System.Reflection.BindingFlags.Static| System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        //MethodInfo mi = t.GetProperty
        object temp1 = t.GetField("Current", bf).GetValue(null);
        object temp2 = t.GetField("_activeSockets", bf).GetValue(temp1);
              
        context.Response.ContentType = "text/plain";        
        context.Response.Write("Active WebSocket Connections="+((System.Collections.Generic.HashSet<System.Net.WebSockets.WebSocket>)temp2).Count);
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}