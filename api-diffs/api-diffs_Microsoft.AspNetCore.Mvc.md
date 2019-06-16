# Microsoft.AspNetCore.Mvc

``` diff
 namespace Microsoft.AspNetCore.Mvc {
     public class AcceptedAtActionResult : ObjectResult {
         public AcceptedAtActionResult(string actionName, string controllerName, object routeValues, object value);
         public string ActionName { get; set; }
         public string ControllerName { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public class AcceptedAtRouteResult : ObjectResult {
         public AcceptedAtRouteResult(object routeValues, object value);
         public AcceptedAtRouteResult(string routeName, object routeValues, object value);
         public string RouteName { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public class AcceptedResult : ObjectResult {
         public AcceptedResult();
         public AcceptedResult(string location, object value);
         public AcceptedResult(Uri locationUri, object value);
         public string Location { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public sealed class AcceptVerbsAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider {
         public AcceptVerbsAttribute(string method);
         public AcceptVerbsAttribute(params string[] methods);
         public IEnumerable<string> HttpMethods { get; }
         Nullable<int> Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get; }
         string Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Template { get; }
         public string Name { get; set; }
         public int Order { get; set; }
         public string Route { get; set; }
     }
     public class ActionContext {
         public ActionContext();
         public ActionContext(HttpContext httpContext, RouteData routeData, ActionDescriptor actionDescriptor);
         public ActionContext(HttpContext httpContext, RouteData routeData, ActionDescriptor actionDescriptor, ModelStateDictionary modelState);
         public ActionContext(ActionContext actionContext);
         public ActionDescriptor ActionDescriptor { get; set; }
         public HttpContext HttpContext { get; set; }
         public ModelStateDictionary ModelState { get; }
         public RouteData RouteData { get; set; }
     }
     public class ActionContextAttribute : Attribute {
         public ActionContextAttribute();
     }
     public sealed class ActionNameAttribute : Attribute {
         public ActionNameAttribute(string name);
         public string Name { get; }
     }
     public abstract class ActionResult : IActionResult {
         protected ActionResult();
         public virtual void ExecuteResult(ActionContext context);
         public virtual Task ExecuteResultAsync(ActionContext context);
     }
     public sealed class ActionResult<TValue> : IConvertToActionResult {
         public ActionResult(ActionResult result);
         public ActionResult(TValue value);
         public ActionResult Result { get; }
         public TValue Value { get; }
         IActionResult Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult.Convert();
         public static implicit operator ActionResult<TValue> (ActionResult result);
         public static implicit operator ActionResult<TValue> (TValue value);
     }
     public class AntiforgeryValidationFailedResult : BadRequestResult, IActionResult, IAntiforgeryValidationFailedResult {
         public AntiforgeryValidationFailedResult();
     }
     public class ApiBehaviorOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public ApiBehaviorOptions();
-        public bool AllowInferringBindingSourceForCollectionTypesAsFromQuery { get; set; }

         public IDictionary<int, ClientErrorData> ClientErrorMapping { get; }
         public Func<ActionContext, IActionResult> InvalidModelStateResponseFactory { get; set; }
         public bool SuppressConsumesConstraintForFormFileParameters { get; set; }
         public bool SuppressInferBindingSourcesForParameters { get; set; }
         public bool SuppressMapClientErrors { get; set; }
         public bool SuppressModelStateInvalidFilter { get; set; }
-        public bool SuppressUseValidationProblemDetailsForInvalidModelStateResponses { get; set; }

         IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class ApiControllerAttribute : ControllerAttribute, IApiBehaviorMetadata, IFilterMetadata {
         public ApiControllerAttribute();
     }
     public sealed class ApiConventionMethodAttribute : Attribute {
         public ApiConventionMethodAttribute(Type conventionType, string methodName);
         public Type ConventionType { get; }
     }
     public sealed class ApiConventionTypeAttribute : Attribute {
         public ApiConventionTypeAttribute(Type conventionType);
         public Type ConventionType { get; }
     }
     public class ApiExplorerSettingsAttribute : Attribute, IApiDescriptionGroupNameProvider, IApiDescriptionVisibilityProvider {
         public ApiExplorerSettingsAttribute();
         public string GroupName { get; set; }
         public bool IgnoreApi { get; set; }
     }
     public class AreaAttribute : RouteValueAttribute {
         public AreaAttribute(string areaName);
     }
     public class AutoValidateAntiforgeryTokenAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public AutoValidateAntiforgeryTokenAttribute();
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class BadRequestObjectResult : ObjectResult {
         public BadRequestObjectResult(ModelStateDictionary modelState);
         public BadRequestObjectResult(object error);
     }
     public class BadRequestResult : StatusCodeResult {
         public BadRequestResult();
     }
     public class BindAttribute : Attribute, IModelNameProvider, IPropertyFilterProvider {
         public BindAttribute(params string[] include);
         public string[] Include { get; }
         string Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider.Name { get; }
         public string Prefix { get; set; }
         public Func<ModelMetadata, bool> PropertyFilter { get; }
     }
     public class BindPropertiesAttribute : Attribute {
         public BindPropertiesAttribute();
         public bool SupportsGet { get; set; }
     }
     public class BindPropertyAttribute : Attribute, IBinderTypeProviderMetadata, IBindingSourceMetadata, IModelNameProvider, IRequestPredicateProvider {
         public BindPropertyAttribute();
         public Type BinderType { get; set; }
         public virtual BindingSource BindingSource { get; protected set; }
         Func<ActionContext, bool> Microsoft.AspNetCore.Mvc.ModelBinding.IRequestPredicateProvider.RequestPredicate { get; }
         public string Name { get; set; }
         public bool SupportsGet { get; set; }
     }
     public class CacheProfile {
         public CacheProfile();
         public Nullable<int> Duration { get; set; }
         public Nullable<ResponseCacheLocation> Location { get; set; }
         public Nullable<bool> NoStore { get; set; }
         public string VaryByHeader { get; set; }
         public string[] VaryByQueryKeys { get; set; }
     }
     public class ChallengeResult : ActionResult {
         public ChallengeResult();
         public ChallengeResult(AuthenticationProperties properties);
         public ChallengeResult(IList<string> authenticationSchemes);
         public ChallengeResult(IList<string> authenticationSchemes, AuthenticationProperties properties);
         public ChallengeResult(string authenticationScheme);
         public ChallengeResult(string authenticationScheme, AuthenticationProperties properties);
         public IList<string> AuthenticationSchemes { get; set; }
         public AuthenticationProperties Properties { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class ClientErrorData {
         public ClientErrorData();
         public string Link { get; set; }
         public string Title { get; set; }
     }
     public enum CompatibilityVersion {
         Latest = 2147483647,
         Version_2_0 = 0,
         Version_2_1 = 1,
         Version_2_2 = 2,
+        Version_3_0 = 3,
     }
     public class ConflictObjectResult : ObjectResult {
         public ConflictObjectResult(ModelStateDictionary modelState);
         public ConflictObjectResult(object error);
     }
     public class ConflictResult : StatusCodeResult {
         public ConflictResult();
     }
-    public class ConsumesAttribute : Attribute, IActionConstraint, IActionConstraintMetadata, IApiRequestMetadataProvider, IConsumesActionConstraint, IFilterMetadata, IResourceFilter {
+    public class ConsumesAttribute : Attribute, IActionConstraint, IActionConstraintMetadata, IApiRequestMetadataProvider, IFilterMetadata, IResourceFilter {
         public static readonly int ConsumesActionConstraintOrder;
         public ConsumesAttribute(string contentType, params string[] otherContentTypes);
         public MediaTypeCollection ContentTypes { get; set; }
         int Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint.Order { get; }
         public bool Accept(ActionConstraintContext context);
         public void OnResourceExecuted(ResourceExecutedContext context);
         public void OnResourceExecuting(ResourceExecutingContext context);
         public void SetContentTypes(MediaTypeCollection contentTypes);
     }
     public class ContentResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public ContentResult();
         public string Content { get; set; }
         public string ContentType { get; set; }
         public Nullable<int> StatusCode { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public abstract class Controller : ControllerBase, IActionFilter, IAsyncActionFilter, IDisposable, IFilterMetadata {
         protected Controller();
         public ITempDataDictionary TempData { get; set; }
         public dynamic ViewBag { get; }
         public ViewDataDictionary ViewData { get; set; }
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         public virtual JsonResult Json(object data);
-        public virtual JsonResult Json(object data, JsonSerializerSettings serializerSettings);

+        public virtual JsonResult Json(object data, object serializerSettings);
         public virtual void OnActionExecuted(ActionExecutedContext context);
         public virtual void OnActionExecuting(ActionExecutingContext context);
         public virtual Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
         public virtual PartialViewResult PartialView();
         public virtual PartialViewResult PartialView(object model);
         public virtual PartialViewResult PartialView(string viewName);
         public virtual PartialViewResult PartialView(string viewName, object model);
         public virtual ViewResult View();
         public virtual ViewResult View(object model);
         public virtual ViewResult View(string viewName);
         public virtual ViewResult View(string viewName, object model);
         public virtual ViewComponentResult ViewComponent(string componentName);
         public virtual ViewComponentResult ViewComponent(string componentName, object arguments);
         public virtual ViewComponentResult ViewComponent(Type componentType);
         public virtual ViewComponentResult ViewComponent(Type componentType, object arguments);
     }
     public class ControllerAttribute : Attribute {
         public ControllerAttribute();
     }
     public abstract class ControllerBase {
         protected ControllerBase();
         public ControllerContext ControllerContext { get; set; }
         public HttpContext HttpContext { get; }
         public IModelMetadataProvider MetadataProvider { get; set; }
         public IModelBinderFactory ModelBinderFactory { get; set; }
         public ModelStateDictionary ModelState { get; }
         public IObjectModelValidator ObjectValidator { get; set; }
         public HttpRequest Request { get; }
         public HttpResponse Response { get; }
         public RouteData RouteData { get; }
         public IUrlHelper Url { get; set; }
         public ClaimsPrincipal User { get; }
         public virtual AcceptedResult Accepted();
         public virtual AcceptedResult Accepted(object value);
         public virtual AcceptedResult Accepted(string uri);
         public virtual AcceptedResult Accepted(string uri, object value);
         public virtual AcceptedResult Accepted(Uri uri);
         public virtual AcceptedResult Accepted(Uri uri, object value);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, object value);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, object routeValues, object value);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName, object routeValues);
         public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName, object routeValues, object value);
         public virtual AcceptedAtRouteResult AcceptedAtRoute(object routeValues);
         public virtual AcceptedAtRouteResult AcceptedAtRoute(object routeValues, object value);
         public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName);
         public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues);
         public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues, object value);
         public virtual BadRequestResult BadRequest();
         public virtual BadRequestObjectResult BadRequest(ModelStateDictionary modelState);
         public virtual BadRequestObjectResult BadRequest(object error);
         public virtual ChallengeResult Challenge();
         public virtual ChallengeResult Challenge(AuthenticationProperties properties);
         public virtual ChallengeResult Challenge(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ChallengeResult Challenge(params string[] authenticationSchemes);
         public virtual ConflictResult Conflict();
         public virtual ConflictObjectResult Conflict(ModelStateDictionary modelState);
         public virtual ConflictObjectResult Conflict(object error);
         public virtual ContentResult Content(string content);
         public virtual ContentResult Content(string content, MediaTypeHeaderValue contentType);
         public virtual ContentResult Content(string content, string contentType);
         public virtual ContentResult Content(string content, string contentType, Encoding contentEncoding);
         public virtual CreatedResult Created(string uri, object value);
         public virtual CreatedResult Created(Uri uri, object value);
         public virtual CreatedAtActionResult CreatedAtAction(string actionName, object value);
         public virtual CreatedAtActionResult CreatedAtAction(string actionName, object routeValues, object value);
         public virtual CreatedAtActionResult CreatedAtAction(string actionName, string controllerName, object routeValues, object value);
         public virtual CreatedAtRouteResult CreatedAtRoute(object routeValues, object value);
         public virtual CreatedAtRouteResult CreatedAtRoute(string routeName, object value);
         public virtual CreatedAtRouteResult CreatedAtRoute(string routeName, object routeValues, object value);
         public virtual FileContentResult File(byte[] fileContents, string contentType);
         public virtual FileContentResult File(byte[] fileContents, string contentType, bool enableRangeProcessing);
         public virtual FileContentResult File(byte[] fileContents, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual FileContentResult File(byte[] fileContents, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, bool enableRangeProcessing);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual FileStreamResult File(Stream fileStream, string contentType);
         public virtual FileStreamResult File(Stream fileStream, string contentType, bool enableRangeProcessing);
         public virtual FileStreamResult File(Stream fileStream, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual FileStreamResult File(Stream fileStream, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, bool enableRangeProcessing);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual VirtualFileResult File(string virtualPath, string contentType);
         public virtual VirtualFileResult File(string virtualPath, string contentType, bool enableRangeProcessing);
         public virtual VirtualFileResult File(string virtualPath, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual VirtualFileResult File(string virtualPath, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, bool enableRangeProcessing);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual ForbidResult Forbid();
         public virtual ForbidResult Forbid(AuthenticationProperties properties);
         public virtual ForbidResult Forbid(AuthenticationProperties properties, params string[] authenticationSchemes);
         public virtual ForbidResult Forbid(params string[] authenticationSchemes);
         public virtual LocalRedirectResult LocalRedirect(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanent(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl);
         public virtual LocalRedirectResult LocalRedirectPreserveMethod(string localUrl);
         public virtual NoContentResult NoContent();
         public virtual NotFoundResult NotFound();
         public virtual NotFoundObjectResult NotFound(object value);
         public virtual OkResult Ok();
         public virtual OkObjectResult Ok(object value);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, bool enableRangeProcessing);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, bool enableRangeProcessing);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag);
         public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, Nullable<DateTimeOffset> lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing);
         public virtual RedirectResult Redirect(string url);
         public virtual RedirectResult RedirectPermanent(string url);
         public virtual RedirectResult RedirectPermanentPreserveMethod(string url);
         public virtual RedirectResult RedirectPreserveMethod(string url);
         public virtual RedirectToActionResult RedirectToAction();
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
         public virtual RedirectToPageResult RedirectToPage(string pageName);
         public virtual RedirectToPageResult RedirectToPage(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment);
         public virtual RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName, string pageHandler = null, object routeValues = null, string fragment = null);
         public virtual RedirectToPageResult RedirectToPagePreserveMethod(string pageName, string pageHandler = null, object routeValues = null, string fragment = null);
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
         public virtual UnauthorizedObjectResult Unauthorized(object value);
         public virtual UnprocessableEntityResult UnprocessableEntity();
         public virtual UnprocessableEntityObjectResult UnprocessableEntity(ModelStateDictionary modelState);
         public virtual UnprocessableEntityObjectResult UnprocessableEntity(object error);
         public virtual ActionResult ValidationProblem();
         public virtual ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary);
         public virtual ActionResult ValidationProblem(ValidationProblemDetails descriptor);
     }
     public class ControllerContext : ActionContext {
         public ControllerContext();
         public ControllerContext(ActionContext context);
         public new ControllerActionDescriptor ActionDescriptor { get; set; }
         public virtual IList<IValueProviderFactory> ValueProviderFactories { get; set; }
     }
     public class ControllerContextAttribute : Attribute {
         public ControllerContextAttribute();
     }
     public class CookieTempDataProviderOptions {
         public CookieTempDataProviderOptions();
         public CookieBuilder Cookie { get; set; }
-        public string CookieName { get; set; }

-        public string Domain { get; set; }

-        public string Path { get; set; }

     }
     public class CreatedAtActionResult : ObjectResult {
         public CreatedAtActionResult(string actionName, string controllerName, object routeValues, object value);
         public string ActionName { get; set; }
         public string ControllerName { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public class CreatedAtRouteResult : ObjectResult {
         public CreatedAtRouteResult(object routeValues, object value);
         public CreatedAtRouteResult(string routeName, object routeValues, object value);
         public string RouteName { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public class CreatedResult : ObjectResult {
         public CreatedResult(string location, object value);
         public CreatedResult(Uri location, object value);
         public string Location { get; set; }
         public override void OnFormatting(ActionContext context);
     }
     public static class DefaultApiConventions {
         public static void Create(object model);
         public static void Delete(object id);
         public static void Edit(object id, object model);
         public static void Find(object id);
         public static void Get(object id);
         public static void Post(object model);
         public static void Put(object id, object model);
         public static void Update(object id, object model);
     }
     public class DisableRequestSizeLimitAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public DisableRequestSizeLimitAttribute();
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class EmptyResult : ActionResult {
         public EmptyResult();
         public override void ExecuteResult(ActionContext context);
     }
     public class FileContentResult : FileResult {
         public FileContentResult(byte[] fileContents, MediaTypeHeaderValue contentType);
         public FileContentResult(byte[] fileContents, string contentType);
         public byte[] FileContents { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public abstract class FileResult : ActionResult {
         protected FileResult(string contentType);
         public string ContentType { get; }
         public bool EnableRangeProcessing { get; set; }
         public EntityTagHeaderValue EntityTag { get; set; }
         public string FileDownloadName { get; set; }
         public Nullable<DateTimeOffset> LastModified { get; set; }
     }
     public class FileStreamResult : FileResult {
         public FileStreamResult(Stream fileStream, MediaTypeHeaderValue contentType);
         public FileStreamResult(Stream fileStream, string contentType);
         public Stream FileStream { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class ForbidResult : ActionResult {
         public ForbidResult();
         public ForbidResult(AuthenticationProperties properties);
         public ForbidResult(IList<string> authenticationSchemes);
         public ForbidResult(IList<string> authenticationSchemes, AuthenticationProperties properties);
         public ForbidResult(string authenticationScheme);
         public ForbidResult(string authenticationScheme, AuthenticationProperties properties);
         public IList<string> AuthenticationSchemes { get; set; }
         public AuthenticationProperties Properties { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class FormatFilterAttribute : Attribute, IFilterFactory, IFilterMetadata {
         public FormatFilterAttribute();
         public bool IsReusable { get; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class FromBodyAttribute : Attribute, IBindingSourceMetadata {
         public FromBodyAttribute();
         public BindingSource BindingSource { get; }
     }
     public class FromFormAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider {
         public FromFormAttribute();
         public BindingSource BindingSource { get; }
         public string Name { get; set; }
     }
     public class FromHeaderAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider {
         public FromHeaderAttribute();
         public BindingSource BindingSource { get; }
         public string Name { get; set; }
     }
     public class FromQueryAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider {
         public FromQueryAttribute();
         public BindingSource BindingSource { get; }
         public string Name { get; set; }
     }
     public class FromRouteAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider {
         public FromRouteAttribute();
         public BindingSource BindingSource { get; }
         public string Name { get; set; }
     }
     public class FromServicesAttribute : Attribute, IBindingSourceMetadata {
         public FromServicesAttribute();
         public BindingSource BindingSource { get; }
     }
     public sealed class HiddenInputAttribute : Attribute {
         public HiddenInputAttribute();
         public bool DisplayValue { get; set; }
     }
     public class HttpDeleteAttribute : HttpMethodAttribute {
         public HttpDeleteAttribute();
         public HttpDeleteAttribute(string template);
     }
     public class HttpGetAttribute : HttpMethodAttribute {
         public HttpGetAttribute();
         public HttpGetAttribute(string template);
     }
     public class HttpHeadAttribute : HttpMethodAttribute {
         public HttpHeadAttribute();
         public HttpHeadAttribute(string template);
     }
     public class HttpOptionsAttribute : HttpMethodAttribute {
         public HttpOptionsAttribute();
         public HttpOptionsAttribute(string template);
     }
     public class HttpPatchAttribute : HttpMethodAttribute {
         public HttpPatchAttribute();
         public HttpPatchAttribute(string template);
     }
     public class HttpPostAttribute : HttpMethodAttribute {
         public HttpPostAttribute();
         public HttpPostAttribute(string template);
     }
     public class HttpPutAttribute : HttpMethodAttribute {
         public HttpPutAttribute();
         public HttpPutAttribute(string template);
     }
     public interface IActionResult {
         Task ExecuteResultAsync(ActionContext context);
     }
     public interface IDesignTimeMvcBuilderConfiguration {
         void ConfigureMvc(IMvcBuilder builder);
     }
     public class IgnoreAntiforgeryTokenAttribute : Attribute, IAntiforgeryPolicy, IFilterMetadata, IOrderedFilter {
         public IgnoreAntiforgeryTokenAttribute();
         public int Order { get; set; }
     }
     public interface IRequestFormLimitsPolicy : IFilterMetadata
     public interface IRequestSizePolicy : IFilterMetadata
     public interface IUrlHelper {
         ActionContext ActionContext { get; }
         string Action(UrlActionContext actionContext);
         string Content(string contentPath);
         bool IsLocalUrl(string url);
         string Link(string routeName, object values);
         string RouteUrl(UrlRouteContext routeContext);
     }
     public interface IViewComponentHelper {
         Task<IHtmlContent> InvokeAsync(string name, object arguments);
         Task<IHtmlContent> InvokeAsync(Type componentType, object arguments);
     }
     public interface IViewComponentResult {
         void Execute(ViewComponentContext context);
         Task ExecuteAsync(ViewComponentContext context);
     }
+    public class JsonOptions {
+        public JsonOptions();
+        public JsonSerializerOptions JsonSerializerOptions { get; }
+    }
-    public static class JsonPatchExtensions {
 {
-        public static void ApplyTo<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo, ModelStateDictionary modelState) where T : class;

-        public static void ApplyTo<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo, ModelStateDictionary modelState, string prefix) where T : class;

-    }
     public class JsonResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public JsonResult(object value);
-        public JsonResult(object value, JsonSerializerSettings serializerSettings);

+        public JsonResult(object value, object serializerSettings);
         public string ContentType { get; set; }
-        public JsonSerializerSettings SerializerSettings { get; set; }
+        public object SerializerSettings { get; set; }
         public Nullable<int> StatusCode { get; set; }
         public object Value { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class LocalRedirectResult : ActionResult {
         public LocalRedirectResult(string localUrl);
         public LocalRedirectResult(string localUrl, bool permanent);
         public LocalRedirectResult(string localUrl, bool permanent, bool preserveMethod);
         public bool Permanent { get; set; }
         public bool PreserveMethod { get; set; }
         public string Url { get; set; }
         public IUrlHelper UrlHelper { get; set; }
-        public override void ExecuteResult(ActionContext context);

         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class MiddlewareFilterAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public MiddlewareFilterAttribute(Type configurationType);
         public Type ConfigurationType { get; }
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class ModelBinderAttribute : Attribute, IBinderTypeProviderMetadata, IBindingSourceMetadata, IModelNameProvider {
         public ModelBinderAttribute();
         public ModelBinderAttribute(Type binderType);
         public Type BinderType { get; set; }
         public virtual BindingSource BindingSource { get; protected set; }
         public string Name { get; set; }
     }
     public class ModelMetadataTypeAttribute : Attribute {
         public ModelMetadataTypeAttribute(Type type);
         public Type MetadataType { get; }
     }
-    public class MvcJsonOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
 {
-        public MvcJsonOptions();

-        public bool AllowInputFormatterExceptionMessages { get; set; }

-        public JsonSerializerSettings SerializerSettings { get; }

-        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
     public class MvcOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public MvcOptions();
-        public bool AllowBindingHeaderValuesToNonStringModelTypes { get; set; }

-        public bool AllowCombiningAuthorizeFilters { get; set; }

         public bool AllowEmptyInputInBodyModelBinding { get; set; }
-        public bool AllowShortCircuitingValidationWhenNoValidatorsArePresent { get; set; }

-        public bool AllowValidatingTopLevelNodes { get; set; }

         public IDictionary<string, CacheProfile> CacheProfiles { get; }
         public IList<IApplicationModelConvention> Conventions { get; }
         public bool EnableEndpointRouting { get; set; }
         public FilterCollection Filters { get; }
         public FormatterMappings FormatterMappings { get; }
-        public InputFormatterExceptionPolicy InputFormatterExceptionPolicy { get; set; }

         public FormatterCollection<IInputFormatter> InputFormatters { get; }
+        public int MaxModelBindingCollectionSize { get; set; }
+        public int MaxModelBindingRecursionDepth { get; set; }
         public int MaxModelValidationErrors { get; set; }
         public Nullable<int> MaxValidationDepth { get; set; }
         public IList<IModelBinderProvider> ModelBinderProviders { get; }
         public DefaultModelBindingMessageProvider ModelBindingMessageProvider { get; }
         public IList<IMetadataDetailsProvider> ModelMetadataDetailsProviders { get; }
         public IList<IModelValidatorProvider> ModelValidatorProviders { get; }
         public FormatterCollection<IOutputFormatter> OutputFormatters { get; }
         public bool RequireHttpsPermanent { get; set; }
         public bool RespectBrowserAcceptHeader { get; set; }
         public bool ReturnHttpNotAcceptable { get; set; }
         public Nullable<int> SslPort { get; set; }
+        public bool SuppressAsyncSuffixInActionNames { get; set; }
-        public bool SuppressBindingUndefinedValueToEnumType { get; set; }

+        public bool SuppressImplicitRequiredAttributeForNonNullableReferenceTypes { get; set; }
         public bool SuppressInputFormatterBuffering { get; set; }
+        public bool SuppressOutputFormatterBuffering { get; set; }
+        public bool ValidateComplexTypesIfChildValidationFails { get; set; }
         public IList<IValueProviderFactory> ValueProviderFactories { get; }
         IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class MvcViewOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public MvcViewOptions();
-        public bool AllowRenderingMaxLengthAttribute { get; set; }

         public IList<IClientModelValidatorProvider> ClientModelValidatorProviders { get; }
         public HtmlHelperOptions HtmlHelperOptions { get; set; }
-        public bool SuppressTempDataAttributePrefix { get; set; }

         public IList<IViewEngine> ViewEngines { get; }
         IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class NoContentResult : StatusCodeResult {
         public NoContentResult();
     }
     public sealed class NonActionAttribute : Attribute {
         public NonActionAttribute();
     }
     public sealed class NonControllerAttribute : Attribute {
         public NonControllerAttribute();
     }
     public class NonViewComponentAttribute : Attribute {
         public NonViewComponentAttribute();
     }
     public class NotFoundObjectResult : ObjectResult {
         public NotFoundObjectResult(object value);
     }
     public class NotFoundResult : StatusCodeResult {
         public NotFoundResult();
     }
     public class ObjectResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public ObjectResult(object value);
         public MediaTypeCollection ContentTypes { get; set; }
         public Type DeclaredType { get; set; }
         public FormatterCollection<IOutputFormatter> Formatters { get; set; }
         public Nullable<int> StatusCode { get; set; }
         public object Value { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
         public virtual void OnFormatting(ActionContext context);
     }
     public class OkObjectResult : ObjectResult {
         public OkObjectResult(object value);
     }
     public class OkResult : StatusCodeResult {
         public OkResult();
     }
+    public class PageRemoteAttribute : RemoteAttributeBase {
+        public PageRemoteAttribute();
+        public string PageHandler { get; set; }
+        public string PageName { get; set; }
+        protected override string GetUrl(ClientModelValidationContext context);
+    }
     public class PartialViewResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public PartialViewResult();
         public string ContentType { get; set; }
         public object Model { get; }
         public Nullable<int> StatusCode { get; set; }
         public ITempDataDictionary TempData { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public IViewEngine ViewEngine { get; set; }
         public string ViewName { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class PhysicalFileResult : FileResult {
         public PhysicalFileResult(string fileName, MediaTypeHeaderValue contentType);
         public PhysicalFileResult(string fileName, string contentType);
         public string FileName { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class ProblemDetails {
         public ProblemDetails();
         public string Detail { get; set; }
         public IDictionary<string, object> Extensions { get; }
         public string Instance { get; set; }
         public Nullable<int> Status { get; set; }
         public string Title { get; set; }
         public string Type { get; set; }
     }
     public class ProducesAttribute : Attribute, IApiResponseMetadataProvider, IFilterMetadata, IOrderedFilter, IResultFilter {
         public ProducesAttribute(string contentType, params string[] additionalContentTypes);
         public ProducesAttribute(Type type);
         public MediaTypeCollection ContentTypes { get; set; }
         public int Order { get; set; }
         public int StatusCode { get; }
         public Type Type { get; set; }
         public virtual void OnResultExecuted(ResultExecutedContext context);
         public virtual void OnResultExecuting(ResultExecutingContext context);
         public void SetContentTypes(MediaTypeCollection contentTypes);
     }
     public sealed class ProducesDefaultResponseTypeAttribute : Attribute, IApiDefaultResponseMetadataProvider, IApiResponseMetadataProvider, IFilterMetadata {
         public ProducesDefaultResponseTypeAttribute();
         public ProducesDefaultResponseTypeAttribute(Type type);
         public int StatusCode { get; }
         public Type Type { get; }
         void Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes);
     }
     public sealed class ProducesErrorResponseTypeAttribute : Attribute {
         public ProducesErrorResponseTypeAttribute(Type type);
         public Type Type { get; }
     }
     public class ProducesResponseTypeAttribute : Attribute, IApiResponseMetadataProvider, IFilterMetadata {
         public ProducesResponseTypeAttribute(int statusCode);
         public ProducesResponseTypeAttribute(Type type, int statusCode);
         public int StatusCode { get; set; }
         public Type Type { get; set; }
         void Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes);
     }
     public class RedirectResult : ActionResult, IActionResult, IKeepTempDataResult {
         public RedirectResult(string url);
         public RedirectResult(string url, bool permanent);
         public RedirectResult(string url, bool permanent, bool preserveMethod);
         public bool Permanent { get; set; }
         public bool PreserveMethod { get; set; }
         public string Url { get; set; }
         public IUrlHelper UrlHelper { get; set; }
-        public override void ExecuteResult(ActionContext context);

         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class RedirectToActionResult : ActionResult, IActionResult, IKeepTempDataResult {
         public RedirectToActionResult(string actionName, string controllerName, object routeValues);
         public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent);
         public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, bool preserveMethod);
         public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, bool preserveMethod, string fragment);
         public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, string fragment);
         public RedirectToActionResult(string actionName, string controllerName, object routeValues, string fragment);
         public string ActionName { get; set; }
         public string ControllerName { get; set; }
         public string Fragment { get; set; }
         public bool Permanent { get; set; }
         public bool PreserveMethod { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
-        public override void ExecuteResult(ActionContext context);

         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class RedirectToPageResult : ActionResult, IActionResult, IKeepTempDataResult {
         public RedirectToPageResult(string pageName);
         public RedirectToPageResult(string pageName, object routeValues);
         public RedirectToPageResult(string pageName, string pageHandler);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, bool preserveMethod);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, bool preserveMethod, string fragment);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, string fragment);
         public RedirectToPageResult(string pageName, string pageHandler, object routeValues, string fragment);
         public string Fragment { get; set; }
         public string Host { get; set; }
         public string PageHandler { get; set; }
         public string PageName { get; set; }
         public bool Permanent { get; set; }
         public bool PreserveMethod { get; set; }
         public string Protocol { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
-        public override void ExecuteResult(ActionContext context);

         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class RedirectToRouteResult : ActionResult, IActionResult, IKeepTempDataResult {
         public RedirectToRouteResult(object routeValues);
         public RedirectToRouteResult(string routeName, object routeValues);
         public RedirectToRouteResult(string routeName, object routeValues, bool permanent);
         public RedirectToRouteResult(string routeName, object routeValues, bool permanent, bool preserveMethod);
         public RedirectToRouteResult(string routeName, object routeValues, bool permanent, bool preserveMethod, string fragment);
         public RedirectToRouteResult(string routeName, object routeValues, bool permanent, string fragment);
         public RedirectToRouteResult(string routeName, object routeValues, string fragment);
         public string Fragment { get; set; }
         public bool Permanent { get; set; }
         public bool PreserveMethod { get; set; }
         public string RouteName { get; set; }
         public RouteValueDictionary RouteValues { get; set; }
         public IUrlHelper UrlHelper { get; set; }
-        public override void ExecuteResult(ActionContext context);

         public override Task ExecuteResultAsync(ActionContext context);
     }
-    public class RemoteAttribute : ValidationAttribute, IClientModelValidator {
+    public class RemoteAttribute : RemoteAttributeBase {
         protected RemoteAttribute();
         public RemoteAttribute(string routeName);
         public RemoteAttribute(string action, string controller);
         public RemoteAttribute(string action, string controller, string areaName);
-        public string AdditionalFields { get; set; }

-        public string HttpMethod { get; set; }

-        protected RouteValueDictionary RouteData { get; }

         protected string RouteName { get; set; }
-        public virtual void AddValidation(ClientModelValidationContext context);

-        public string FormatAdditionalFieldsForClientValidation(string property);

-        public override string FormatErrorMessage(string name);

-        public static string FormatPropertyForClientValidation(string property);

-        protected virtual string GetUrl(ClientModelValidationContext context);
+        protected override string GetUrl(ClientModelValidationContext context);
-        public override bool IsValid(object value);

     }
+    public abstract class RemoteAttributeBase : ValidationAttribute, IClientModelValidator {
+        protected RemoteAttributeBase();
+        public string AdditionalFields { get; set; }
+        public string HttpMethod { get; set; }
+        protected RouteValueDictionary RouteData { get; }
+        public virtual void AddValidation(ClientModelValidationContext context);
+        public string FormatAdditionalFieldsForClientValidation(string property);
+        public override string FormatErrorMessage(string name);
+        public static string FormatPropertyForClientValidation(string property);
+        protected abstract string GetUrl(ClientModelValidationContext context);
+        public override bool IsValid(object value);
+    }
     public class RequestFormLimitsAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public RequestFormLimitsAttribute();
         public bool BufferBody { get; set; }
         public long BufferBodyLengthLimit { get; set; }
         public bool IsReusable { get; }
         public int KeyLengthLimit { get; set; }
         public int MemoryBufferThreshold { get; set; }
         public long MultipartBodyLengthLimit { get; set; }
         public int MultipartBoundaryLengthLimit { get; set; }
         public int MultipartHeadersCountLimit { get; set; }
         public int MultipartHeadersLengthLimit { get; set; }
         public int Order { get; set; }
         public int ValueCountLimit { get; set; }
         public int ValueLengthLimit { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class RequestSizeLimitAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public RequestSizeLimitAttribute(long bytes);
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class RequireHttpsAttribute : Attribute, IAuthorizationFilter, IFilterMetadata, IOrderedFilter {
         public RequireHttpsAttribute();
         public int Order { get; set; }
         public bool Permanent { get; set; }
         protected virtual void HandleNonHttpsRequest(AuthorizationFilterContext filterContext);
         public virtual void OnAuthorization(AuthorizationFilterContext filterContext);
     }
     public class ResponseCacheAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public ResponseCacheAttribute();
         public string CacheProfileName { get; set; }
         public int Duration { get; set; }
         public bool IsReusable { get; }
         public ResponseCacheLocation Location { get; set; }
         public bool NoStore { get; set; }
         public int Order { get; set; }
         public string VaryByHeader { get; set; }
         public string[] VaryByQueryKeys { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
         public CacheProfile GetCacheProfile(MvcOptions options);
     }
     public enum ResponseCacheLocation {
         Any = 0,
         Client = 1,
         None = 2,
     }
     public class RouteAttribute : Attribute, IRouteTemplateProvider {
         public RouteAttribute(string template);
         Nullable<int> Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get; }
         public string Name { get; set; }
         public int Order { get; set; }
         public string Template { get; }
     }
     public sealed class SerializableError : Dictionary<string, object> {
         public SerializableError();
         public SerializableError(ModelStateDictionary modelState);
     }
     public class ServiceFilterAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public ServiceFilterAttribute(Type type);
         public bool IsReusable { get; set; }
         public int Order { get; set; }
         public Type ServiceType { get; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class SignInResult : ActionResult {
         public SignInResult(string authenticationScheme, ClaimsPrincipal principal);
         public SignInResult(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties);
         public string AuthenticationScheme { get; set; }
         public ClaimsPrincipal Principal { get; set; }
         public AuthenticationProperties Properties { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class SignOutResult : ActionResult {
         public SignOutResult();
         public SignOutResult(IList<string> authenticationSchemes);
         public SignOutResult(IList<string> authenticationSchemes, AuthenticationProperties properties);
         public SignOutResult(string authenticationScheme);
         public SignOutResult(string authenticationScheme, AuthenticationProperties properties);
         public IList<string> AuthenticationSchemes { get; set; }
         public AuthenticationProperties Properties { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class SkipStatusCodePagesAttribute : Attribute, IFilterMetadata, IResourceFilter {
         public SkipStatusCodePagesAttribute();
         public void OnResourceExecuted(ResourceExecutedContext context);
         public void OnResourceExecuting(ResourceExecutingContext context);
     }
     public class StatusCodeResult : ActionResult, IActionResult, IClientErrorActionResult, IStatusCodeActionResult {
         public StatusCodeResult(int statusCode);
         Nullable<int> Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult.StatusCode { get; }
         public int StatusCode { get; }
         public override void ExecuteResult(ActionContext context);
     }
     public sealed class TempDataAttribute : Attribute {
         public TempDataAttribute();
         public string Key { get; set; }
     }
     public class TypeFilterAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public TypeFilterAttribute(Type type);
         public object[] Arguments { get; set; }
         public Type ImplementationType { get; }
         public bool IsReusable { get; set; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class UnauthorizedObjectResult : ObjectResult {
         public UnauthorizedObjectResult(object value);
     }
     public class UnauthorizedResult : StatusCodeResult {
         public UnauthorizedResult();
     }
     public class UnprocessableEntityObjectResult : ObjectResult {
         public UnprocessableEntityObjectResult(ModelStateDictionary modelState);
         public UnprocessableEntityObjectResult(object error);
     }
     public class UnprocessableEntityResult : StatusCodeResult {
         public UnprocessableEntityResult();
     }
     public class UnsupportedMediaTypeResult : StatusCodeResult {
         public UnsupportedMediaTypeResult();
     }
     public static class UrlHelperExtensions {
         public static string Action(this IUrlHelper helper);
         public static string Action(this IUrlHelper helper, string action);
         public static string Action(this IUrlHelper helper, string action, object values);
         public static string Action(this IUrlHelper helper, string action, string controller);
         public static string Action(this IUrlHelper helper, string action, string controller, object values);
         public static string Action(this IUrlHelper helper, string action, string controller, object values, string protocol);
         public static string Action(this IUrlHelper helper, string action, string controller, object values, string protocol, string host);
         public static string Action(this IUrlHelper helper, string action, string controller, object values, string protocol, string host, string fragment);
+        public static string ActionLink(this IUrlHelper helper, string action = null, string controller = null, object values = null, string protocol = null, string host = null, string fragment = null);
         public static string Page(this IUrlHelper urlHelper, string pageName);
         public static string Page(this IUrlHelper urlHelper, string pageName, object values);
         public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler);
         public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler, object values);
         public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol);
         public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol, string host);
         public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol, string host, string fragment);
+        public static string PageLink(this IUrlHelper urlHelper, string pageName = null, string pageHandler = null, object values = null, string protocol = null, string host = null, string fragment = null);
         public static string RouteUrl(this IUrlHelper helper, object values);
         public static string RouteUrl(this IUrlHelper helper, string routeName);
         public static string RouteUrl(this IUrlHelper helper, string routeName, object values);
         public static string RouteUrl(this IUrlHelper helper, string routeName, object values, string protocol);
         public static string RouteUrl(this IUrlHelper helper, string routeName, object values, string protocol, string host);
         public static string RouteUrl(this IUrlHelper helper, string routeName, object values, string protocol, string host, string fragment);
     }
     public class ValidateAntiForgeryTokenAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public ValidateAntiForgeryTokenAttribute();
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class ValidationProblemDetails : ProblemDetails {
         public ValidationProblemDetails();
         public ValidationProblemDetails(ModelStateDictionary modelState);
         public ValidationProblemDetails(IDictionary<string, string[]> errors);
         public IDictionary<string, string[]> Errors { get; }
     }
     public abstract class ViewComponent {
         protected ViewComponent();
         public HttpContext HttpContext { get; }
         public ModelStateDictionary ModelState { get; }
         public HttpRequest Request { get; }
         public RouteData RouteData { get; }
         public ITempDataDictionary TempData { get; }
         public IUrlHelper Url { get; set; }
         public IPrincipal User { get; }
         public ClaimsPrincipal UserClaimsPrincipal { get; }
         public dynamic ViewBag { get; }
         public ViewComponentContext ViewComponentContext { get; set; }
         public ViewContext ViewContext { get; }
         public ViewDataDictionary ViewData { get; }
         public ICompositeViewEngine ViewEngine { get; set; }
         public ContentViewComponentResult Content(string content);
         public ViewViewComponentResult View();
         public ViewViewComponentResult View(string viewName);
         public ViewViewComponentResult View<TModel>(string viewName, TModel model);
         public ViewViewComponentResult View<TModel>(TModel model);
     }
     public class ViewComponentAttribute : Attribute {
         public ViewComponentAttribute();
         public string Name { get; set; }
     }
     public class ViewComponentResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public ViewComponentResult();
         public object Arguments { get; set; }
         public string ContentType { get; set; }
         public object Model { get; }
         public Nullable<int> StatusCode { get; set; }
         public ITempDataDictionary TempData { get; set; }
         public string ViewComponentName { get; set; }
         public Type ViewComponentType { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public sealed class ViewDataAttribute : Attribute {
         public ViewDataAttribute();
         public string Key { get; set; }
     }
     public class ViewResult : ActionResult, IActionResult, IStatusCodeActionResult {
         public ViewResult();
         public string ContentType { get; set; }
         public object Model { get; }
         public Nullable<int> StatusCode { get; set; }
         public ITempDataDictionary TempData { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public IViewEngine ViewEngine { get; set; }
         public string ViewName { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
     public class VirtualFileResult : FileResult {
         public VirtualFileResult(string fileName, MediaTypeHeaderValue contentType);
         public VirtualFileResult(string fileName, string contentType);
         public string FileName { get; set; }
         public IFileProvider FileProvider { get; set; }
         public override Task ExecuteResultAsync(ActionContext context);
     }
 }
```

