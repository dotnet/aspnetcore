# Microsoft.AspNetCore.Mvc.RazorPages

``` diff
 namespace Microsoft.AspNetCore.Mvc.RazorPages {
     public class CompiledPageActionDescriptor : PageActionDescriptor {
         public CompiledPageActionDescriptor();
         public CompiledPageActionDescriptor(PageActionDescriptor actionDescriptor);
         public TypeInfo DeclaredModelTypeInfo { get; set; }
+        public Endpoint Endpoint { get; set; }
         public IList<HandlerMethodDescriptor> HandlerMethods { get; set; }
         public TypeInfo HandlerTypeInfo { get; set; }
         public TypeInfo ModelTypeInfo { get; set; }
         public TypeInfo PageTypeInfo { get; set; }
     }
     public interface IPageActivatorProvider {
         Func<PageContext, ViewContext, object> CreateActivator(CompiledPageActionDescriptor descriptor);
         Action<PageContext, ViewContext, object> CreateReleaser(CompiledPageActionDescriptor descriptor);
     }
     public interface IPageFactoryProvider {
         Action<PageContext, ViewContext, object> CreatePageDisposer(CompiledPageActionDescriptor descriptor);
         Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor descriptor);
     }
     public interface IPageModelActivatorProvider {
         Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor);
         Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor descriptor);
     }
     public interface IPageModelFactoryProvider {
         Action<PageContext, object> CreateModelDisposer(CompiledPageActionDescriptor descriptor);
         Func<PageContext, object> CreateModelFactory(CompiledPageActionDescriptor descriptor);
     }
     public class NonHandlerAttribute : Attribute {
         public NonHandlerAttribute();
     }
     public abstract class Page : PageBase {
         protected Page();
     }
     public class PageActionDescriptor : ActionDescriptor {
         public PageActionDescriptor();
         public PageActionDescriptor(PageActionDescriptor other);
         public string AreaName { get; set; }
         public override string DisplayName { get; set; }
         public string RelativePath { get; set; }
         public string ViewEnginePath { get; set; }
     }
     public abstract class PageBase : RazorPageBase {
         protected PageBase();
         public HttpContext HttpContext { get; }
         public ModelStateDictionary ModelState { get; }
         public PageContext PageContext { get; set; }
         public HttpRequest Request { get; }
         public HttpResponse Response { get; }
         public RouteData RouteData { get; }
         public override ViewContext ViewContext { get; set; }
         public virtual BadRequestResult BadRequest();
         public virtual BadRequestObjectResult BadRequest(ModelStateDictionary modelState);
         public virtual BadRequestObjectResult BadRequest(object error);
         public override void BeginContext(int position, int length, bool isLiteral);
         public virtual ChallengeResult Challenge();
         public virtual ChallengeResult Challenge(AuthenticationProperties properties);
         public virtual ChallengeResult Challenge(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ChallengeResult Challenge(params string[] authenticationSchemes);
         public virtual ContentResult Content(string content);
         public virtual ContentResult Content(string content, MediaTypeHeaderValue contentType);
         public virtual ContentResult Content(string content, string contentType);
         public virtual ContentResult Content(string content, string contentType, Encoding contentEncoding);
         public override void EndContext();
         public override void EnsureRenderedBodyOrSections();
         public virtual FileContentResult File(byte[] fileContents, string contentType);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName);
         public virtual FileStreamResult File(Stream fileStream, string contentType);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName);
         public virtual VirtualFileResult File(string virtualPath, string contentType);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName);
         public virtual ForbidResult Forbid();
         public virtual ForbidResult Forbid(AuthenticationProperties properties);
         public virtual ForbidResult Forbid(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ForbidResult Forbid(params string[] authenticationSchemes);
         public virtual LocalRedirectResult LocalRedirect(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanent(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPreserveMethod(string localUrl);
         public virtual NotFoundResult NotFound();
         public virtual NotFoundObjectResult NotFound(object value);
         public virtual PageResult Page();
         public virtual PartialViewResult Partial(string viewName);
         public virtual PartialViewResult Partial(string viewName, object model);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName);
         public virtual RedirectResult Redirect(string url);
         public virtual RedirectResult RedirectPermanent(string url);
         public virtual RedirectResult RedirectPermanentPreserveMethod(string url);
         public virtual RedirectResult RedirectPreserveMethod(string url);
         public virtual RedirectToActionResult RedirectToAction(string actionName);
         public virtual RedirectToActionResult RedirectToAction(string actionName, object routeValues);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues, string fragment);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanentPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToActionResult RedirectToActionPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToPageResult RedirectToPage();
         public virtual RedirectToPageResult RedirectToPage(object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName);
         public virtual RedirectToPageResult RedirectToPage(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null);
         public virtual RedirectToPageResult RedirectToPagePreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null);
         public virtual RedirectToRouteResult RedirectToRoute(object routeValues);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues, string fragment);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(object routeValues);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(string routeName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToRouteResult RedirectToRoutePreserveMethod(string routeName = null, object routeValues = null, string fragment = null);
         public virtual SignInResult SignIn(ClaimsPrincipal principal, AuthenticationProperties properties, string authenticationScheme);
         public virtual SignInResult SignIn(ClaimsPrincipal principal, string authenticationScheme);
         public virtual SignOutResult SignOut(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual SignOutResult SignOut(params string[] authenticationSchemes);
         public virtual StatusCodeResult StatusCode(int statusCode);
         public virtual ObjectResult StatusCode(int statusCode, object value);
         public virtual Task<bool> TryUpdateModelAsync(object model, Type modelType, string prefix);
         public Task<bool> TryUpdateModelAsync(object model, Type modelType, string prefix, IValueProvider valueProvider, Func<ModelMetadata, bool> propertyFilter);
         public virtual Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class;
         public virtual Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix) where TModel : class;
         public virtual Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, IValueProvider valueProvider) where TModel : class;
         public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, IValueProvider valueProvider, Func<ModelMetadata, bool> propertyFilter) where TModel : class;
         public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, IValueProvider valueProvider, params Expression<Func<TModel, object>>[] includeExpressions) where TModel : class;
         public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Func<ModelMetadata, bool> propertyFilter) where TModel : class;
         public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, params Expression<Func<TModel, object>>[] includeExpressions) where TModel : class;
         public virtual bool TryValidateModel(object model);
         public virtual bool TryValidateModel(object model, string prefix);
         public virtual UnauthorizedResult Unauthorized();
         public virtual ViewComponentResult ViewComponent(string componentName);
         public virtual ViewComponentResult ViewComponent(string componentName, object arguments);
         public virtual ViewComponentResult ViewComponent(Type componentType);
         public virtual ViewComponentResult ViewComponent(Type componentType, object arguments);
     }
     public class PageContext : ActionContext {
         public PageContext();
         public PageContext(ActionContext actionContext);
         public virtual new CompiledPageActionDescriptor ActionDescriptor { get; set; }
         public virtual IList<IValueProviderFactory> ValueProviderFactories { get; set; }
         public virtual ViewDataDictionary ViewData { get; set; }
         public virtual IList<Func<IRazorPage>> ViewStartFactories { get; set; }
     }
     public class PageContextAttribute : Attribute {
         public PageContextAttribute();
     }
     public abstract class PageModel : IAsyncPageFilter, IFilterMetadata, IPageFilter {
         protected PageModel();
         public HttpContext HttpContext { get; }
         public ModelStateDictionary ModelState { get; }
         public PageContext PageContext { get; set; }
         public HttpRequest Request { get; }
         public HttpResponse Response { get; }
         public RouteData RouteData { get; }
         public ITempDataDictionary TempData { get; set; }
         public IUrlHelper Url { get; set; }
         public ClaimsPrincipal User { get; }
         public ViewDataDictionary ViewData { get; }
         public virtual BadRequestResult BadRequest();
         public virtual BadRequestObjectResult BadRequest(ModelStateDictionary modelState);
         public virtual BadRequestObjectResult BadRequest(object error);
         public virtual ChallengeResult Challenge();
         public virtual ChallengeResult Challenge(AuthenticationProperties properties);
         public virtual ChallengeResult Challenge(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ChallengeResult Challenge(params string[] authenticationSchemes);
         public virtual ContentResult Content(string content);
         public virtual ContentResult Content(string content, MediaTypeHeaderValue contentType);
         public virtual ContentResult Content(string content, string contentType);
         public virtual ContentResult Content(string content, string contentType, Encoding contentEncoding);
         public virtual FileContentResult File(byte[] fileContents, string contentType);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName);
         public virtual FileStreamResult File(Stream fileStream, string contentType);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName);
         public virtual VirtualFileResult File(string virtualPath, string contentType);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName);
         public virtual ForbidResult Forbid();
         public virtual ForbidResult Forbid(AuthenticationProperties properties);
         public virtual ForbidResult Forbid(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ForbidResult Forbid(params string[] authenticationSchemes);
         public virtual LocalRedirectResult LocalRedirect(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanent(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPreserveMethod(string localUrl);
         public virtual NotFoundResult NotFound();
         public virtual NotFoundObjectResult NotFound(object value);
         public virtual void OnPageHandlerExecuted(PageHandlerExecutedContext context);
         public virtual void OnPageHandlerExecuting(PageHandlerExecutingContext context);
         public virtual Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next);
         public virtual void OnPageHandlerSelected(PageHandlerSelectedContext context);
         public virtual Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context);
         public virtual PageResult Page();
         public virtual PartialViewResult Partial(string viewName);
         public virtual PartialViewResult Partial(string viewName, object model);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName);
         protected internal RedirectResult Redirect(string url);
         public virtual RedirectResult RedirectPermanent(string url);
         public virtual RedirectResult RedirectPermanentPreserveMethod(string url);
         public virtual RedirectResult RedirectPreserveMethod(string url);
         public virtual RedirectToActionResult RedirectToAction(string actionName);
         public virtual RedirectToActionResult RedirectToAction(string actionName, object routeValues);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues, string fragment);
         public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, string fragment);
         public virtual RedirectToActionResult RedirectToActionPermanentPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToActionResult RedirectToActionPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToPageResult RedirectToPage();
         public virtual RedirectToPageResult RedirectToPage(object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName);
         public virtual RedirectToPageResult RedirectToPage(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null);
         public virtual RedirectToPageResult RedirectToPagePreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null);
         public virtual RedirectToRouteResult RedirectToRoute(object routeValues);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues, string fragment);
         public virtual RedirectToRouteResult RedirectToRoute(string routeName, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(object routeValues);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment);
         public virtual RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(string routeName = null, object routeValues = null, string fragment = null);
         public virtual RedirectToRouteResult RedirectToRoutePreserveMethod(string routeName = null, object routeValues = null, string fragment = null);
         public virtual SignInResult SignIn(ClaimsPrincipal principal, AuthenticationProperties properties, string authenticationScheme);
         public virtual SignInResult SignIn(ClaimsPrincipal principal, string authenticationScheme);
         public virtual SignOutResult SignOut(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual SignOutResult SignOut(params string[] authenticationSchemes);
         public virtual StatusCodeResult StatusCode(int statusCode);
         public virtual ObjectResult StatusCode(int statusCode, object value);
         protected internal Task<bool> TryUpdateModelAsync(object model, Type modelType, string name);
         protected internal Task<bool> TryUpdateModelAsync(object model, Type modelType, string name, IValueProvider valueProvider, Func<ModelMetadata, bool> propertyFilter);
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, IValueProvider valueProvider) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, IValueProvider valueProvider, Func<ModelMetadata, bool> propertyFilter) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, IValueProvider valueProvider, params Expression<Func<TModel, object>>[] includeExpressions) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, Func<ModelMetadata, bool> propertyFilter) where TModel : class;
         protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, params Expression<Func<TModel, object>>[] includeExpressions) where TModel : class;
         public virtual bool TryValidateModel(object model);
         public virtual bool TryValidateModel(object model, string name);
         public virtual UnauthorizedResult Unauthorized();
         public virtual ViewComponentResult ViewComponent(string componentName);
         public virtual ViewComponentResult ViewComponent(string componentName, object arguments);
         public virtual ViewComponentResult ViewComponent(Type componentType);
         public virtual ViewComponentResult ViewComponent(Type componentType, object arguments);
     }
     public class PageResult : ActionResult {
         public PageResult();
         public string ContentType { get; set; }
         public object Model { get; }
         public PageBase Page { get; set; }
         public Nullable<int> StatusCode { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class RazorPagesOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public RazorPagesOptions();
-        public bool AllowAreas { get; set; }

-        public bool AllowDefaultHandlingForOptionsRequests { get; set; }

-        public bool AllowMappingHeadRequestsToGetHandler { get; set; }

         public PageConventionCollection Conventions { get; }
         public string RootDirectory { get; set; }
         IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
 }
```

