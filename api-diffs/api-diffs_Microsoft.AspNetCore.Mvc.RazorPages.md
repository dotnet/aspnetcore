# Microsoft.AspNetCore.Mvc.RazorPages

``` diff
 namespace Microsoft.AspNetCore.Mvc.RazorPages {
     public class CompiledPageActionDescriptor : PageActionDescriptor {
+        public Endpoint Endpoint { get; set; }
     }
     public class RazorPagesOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowAreas { get; set; }

-        public bool AllowDefaultHandlingForOptionsRequests { get; set; }

-        public bool AllowMappingHeadRequestsToGetHandler { get; set; }

     }
 }
```

