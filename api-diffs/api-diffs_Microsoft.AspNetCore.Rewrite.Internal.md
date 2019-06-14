# Microsoft.AspNetCore.Rewrite.Internal

``` diff
 namespace Microsoft.AspNetCore.Rewrite.Internal {
     public class RedirectToWwwRule : IRule {
+        public readonly string[] _domains;
+        public RedirectToWwwRule(int statusCode, params string[] domains);
     }
 }
```

