# Microsoft.AspNetCore.SpaServices.Prerendering

``` diff
-namespace Microsoft.AspNetCore.SpaServices.Prerendering {
 {
-    public interface ISpaPrerenderer {
 {
-        Task<RenderToStringResult> RenderToString(string moduleName, string exportName = null, object customDataParameter = null, int timeoutMilliseconds = 0);

-    }
-    public interface ISpaPrerendererBuilder {
 {
-        Task Build(ISpaBuilder spaBuilder);

-    }
-    public class JavaScriptModuleExport {
 {
-        public JavaScriptModuleExport(string moduleName);

-        public string ExportName { get; set; }

-        public string ModuleName { get; private set; }

-    }
-    public static class Prerenderer {
 {
-        public static Task<RenderToStringResult> RenderToString(string applicationBasePath, INodeServices nodeServices, CancellationToken applicationStoppingToken, JavaScriptModuleExport bootModule, string requestAbsoluteUrl, string requestPathAndQuery, object customDataParameter, int timeoutMilliseconds, string requestPathBase);

-    }
-    public class PrerenderTagHelper : TagHelper {
 {
-        public PrerenderTagHelper(IServiceProvider serviceProvider);

-        public object CustomDataParameter { get; set; }

-        public string ExportName { get; set; }

-        public string ModuleName { get; set; }

-        public int TimeoutMillisecondsParameter { get; set; }

-        public ViewContext ViewContext { get; set; }

-        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);

-    }
-    public class RenderToStringResult {
 {
-        public RenderToStringResult();

-        public JObject Globals { get; set; }

-        public string Html { get; set; }

-        public string RedirectUrl { get; set; }

-        public Nullable<int> StatusCode { get; set; }

-        public string CreateGlobalsAssignmentScript();

-    }
-}
```

