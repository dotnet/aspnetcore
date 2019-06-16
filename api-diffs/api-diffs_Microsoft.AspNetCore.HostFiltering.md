# Microsoft.AspNetCore.HostFiltering

``` diff
 namespace Microsoft.AspNetCore.HostFiltering {
     public class HostFilteringMiddleware {
         public HostFilteringMiddleware(RequestDelegate next, ILogger<HostFilteringMiddleware> logger, IOptionsMonitor<HostFilteringOptions> optionsMonitor);
         public Task Invoke(HttpContext context);
     }
     public class HostFilteringOptions {
         public HostFilteringOptions();
         public IList<string> AllowedHosts { get; set; }
         public bool AllowEmptyHosts { get; set; }
         public bool IncludeFailureMessage { get; set; }
     }
 }
```

