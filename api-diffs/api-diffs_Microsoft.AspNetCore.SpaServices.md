# Microsoft.AspNetCore.SpaServices

``` diff
-namespace Microsoft.AspNetCore.SpaServices {
 {
-    public interface ISpaBuilder {
 {
-        IApplicationBuilder ApplicationBuilder { get; }

-        SpaOptions Options { get; }

-    }
-    public class SpaOptions {
 {
-        public SpaOptions();

-        public PathString DefaultPage { get; set; }

-        public StaticFileOptions DefaultPageStaticFileOptions { get; set; }

-        public string SourcePath { get; set; }

-        public TimeSpan StartupTimeout { get; set; }

-    }
-}
```

