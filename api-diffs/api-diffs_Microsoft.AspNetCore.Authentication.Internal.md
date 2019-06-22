# Microsoft.AspNetCore.Authentication.Internal

``` diff
-namespace Microsoft.AspNetCore.Authentication.Internal {
 {
-    public class RequestPathBaseCookieBuilder : CookieBuilder {
 {
-        public RequestPathBaseCookieBuilder();

-        protected virtual string AdditionalPath { get; }

-        public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom);

-    }
-}
```

