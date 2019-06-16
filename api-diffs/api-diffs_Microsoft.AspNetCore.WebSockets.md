# Microsoft.AspNetCore.WebSockets

``` diff
 namespace Microsoft.AspNetCore.WebSockets {
     public class ExtendedWebSocketAcceptContext : WebSocketAcceptContext {
         public ExtendedWebSocketAcceptContext();
         public Nullable<TimeSpan> KeepAliveInterval { get; set; }
         public Nullable<int> ReceiveBufferSize { get; set; }
         public override string SubProtocol { get; set; }
     }
     public class WebSocketMiddleware {
-        public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options);

         public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options, ILoggerFactory loggerFactory);
         public Task Invoke(HttpContext context);
     }
     public static class WebSocketsDependencyInjectionExtensions {
         public static IServiceCollection AddWebSockets(this IServiceCollection services, Action<WebSocketOptions> configure);
     }
 }
```

