# Microsoft.AspNetCore.SpaServices.AngularCli

``` diff
-namespace Microsoft.AspNetCore.SpaServices.AngularCli {
 {
-    public class AngularCliBuilder : ISpaPrerendererBuilder {
 {
-        public AngularCliBuilder(string npmScript);

-        public Task Build(ISpaBuilder spaBuilder);

-    }
-    public static class AngularCliMiddlewareExtensions {
 {
-        public static void UseAngularCliServer(this ISpaBuilder spaBuilder, string npmScript);

-    }
-}
```

