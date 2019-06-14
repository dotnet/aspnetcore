# Microsoft.AspNetCore.Mvc.Razor

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor {
     public class RazorView : IView {
+        public RazorView(IRazorViewEngine viewEngine, IRazorPageActivator pageActivator, IReadOnlyList<IRazorPage> viewStartPages, IRazorPage razorPage, HtmlEncoder htmlEncoder, DiagnosticListener diagnosticListener);
-        public RazorView(IRazorViewEngine viewEngine, IRazorPageActivator pageActivator, IReadOnlyList<IRazorPage> viewStartPages, IRazorPage razorPage, HtmlEncoder htmlEncoder, DiagnosticSource diagnosticSource);

     }
     public class RazorViewEngine : IRazorViewEngine, IViewEngine {
-        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, RazorProject razorProject, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource);

-        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, RazorProjectFileSystem razorFileSystem, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource);

+        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, ILoggerFactory loggerFactory, DiagnosticListener diagnosticListener);
     }
-    public class RazorViewEngineOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
+    public class RazorViewEngineOptions {
-        public IList<MetadataReference> AdditionalCompilationReferences { get; }

-        public bool AllowRecompilingViewsOnFileChange { get; set; }

-        public Action<RoslynCompilationContext> CompilationCallback { get; set; }

-        public IList<IFileProvider> FileProviders { get; }

-        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

     }
 }
```

