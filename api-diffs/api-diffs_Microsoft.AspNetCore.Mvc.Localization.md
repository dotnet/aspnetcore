# Microsoft.AspNetCore.Mvc.Localization

``` diff
 namespace Microsoft.AspNetCore.Mvc.Localization {
     public class ViewLocalizer : IHtmlLocalizer, IViewContextAware, IViewLocalizer {
-        public ViewLocalizer(IHtmlLocalizerFactory localizerFactory, IHostingEnvironment hostingEnvironment);

+        public ViewLocalizer(IHtmlLocalizerFactory localizerFactory, IWebHostEnvironment hostingEnvironment);
     }
 }
```

