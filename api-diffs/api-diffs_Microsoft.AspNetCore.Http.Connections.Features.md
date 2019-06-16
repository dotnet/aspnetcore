# Microsoft.AspNetCore.Http.Connections.Features

``` diff
 namespace Microsoft.AspNetCore.Http.Connections.Features {
     public interface IHttpContextFeature {
         HttpContext HttpContext { get; set; }
     }
     public interface IHttpTransportFeature {
         HttpTransportType TransportType { get; }
     }
 }
```

