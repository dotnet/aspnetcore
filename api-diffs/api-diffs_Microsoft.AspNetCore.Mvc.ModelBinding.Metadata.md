# Microsoft.AspNetCore.Mvc.ModelBinding.Metadata

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata {
     public class ValidationMetadataProviderContext {
+        public IReadOnlyList<object> ParameterAttributes { get; }
     }
 }
```

