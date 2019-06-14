# Microsoft.AspNetCore.Identity.UI.V3.Pages.Internal

``` diff
-namespace Microsoft.AspNetCore.Identity.UI.V3.Pages.Internal {
 {
-    public class Areas_Identity_Pages__Layout : RazorPage<object> {
 {
-        public Areas_Identity_Pages__Layout();

-        public IViewComponentHelper Component { get; private set; }

-        public ICompositeViewEngine Engine { get; private set; }

-        public IHostingEnvironment Environment { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages__ValidationScriptsPartial : RazorPage<object> {
 {
-        public Areas_Identity_Pages__ValidationScriptsPartial();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages__ViewImports : RazorPage<object> {
 {
-        public Areas_Identity_Pages__ViewImports();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages__ViewStart : RazorPage<object> {
 {
-        public Areas_Identity_Pages__ViewStart();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Error : Page {
 {
-        public Areas_Identity_Pages_Error();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ErrorModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ErrorModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ErrorModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class ErrorModel : PageModel {
 {
-        public ErrorModel();

-        public string RequestId { get; set; }

-        public bool ShowRequestId { get; }

-        public void OnGet();

-    }
-}
```

