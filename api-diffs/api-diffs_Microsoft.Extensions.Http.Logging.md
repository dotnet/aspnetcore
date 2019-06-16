# Microsoft.Extensions.Http.Logging

``` diff
 namespace Microsoft.Extensions.Http.Logging {
     public class LoggingHttpMessageHandler : DelegatingHandler {
         public LoggingHttpMessageHandler(ILogger logger);
         protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
     }
     public class LoggingScopeHttpMessageHandler : DelegatingHandler {
         public LoggingScopeHttpMessageHandler(ILogger logger);
         protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
     }
 }
```

