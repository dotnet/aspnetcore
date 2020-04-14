// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class ControllerActionEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        internal ControllerActionEndpointConventionBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public static partial class ControllerEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder MapAreaControllerRoute(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string name, string areaName, string pattern, object defaults = null, object constraints = null, object dataTokens = null) { throw null; }
        public static Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder MapControllerRoute(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string name, string pattern, object defaults = null, object constraints = null, object dataTokens = null) { throw null; }
        public static Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder MapControllers(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
        public static Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder MapDefaultControllerRoute(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
        public static void MapDynamicControllerRoute<TTransformer>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern) where TTransformer : Microsoft.AspNetCore.Mvc.Routing.DynamicRouteValueTransformer { }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToAreaController(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string action, string controller, string area) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToAreaController(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string action, string controller, string area) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToController(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string action, string controller) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToController(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string action, string controller) { throw null; }
    }
    public static partial class MvcApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseMvc(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseMvc(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Routing.IRouteBuilder> configureRoutes) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseMvcWithDefaultRoute(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class MvcAreaRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapAreaRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string areaName, string template) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapAreaRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapAreaRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults, object constraints) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapAreaRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults, object constraints, object dataTokens) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc
{
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(202)]
    public partial class AcceptedAtActionResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public AcceptedAtActionResult(string actionName, string controllerName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public string ActionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(202)]
    public partial class AcceptedAtRouteResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public AcceptedAtRouteResult(object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public AcceptedAtRouteResult(string routeName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(202)]
    public partial class AcceptedResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public AcceptedResult() : base (default(object)) { }
        public AcceptedResult(string location, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public AcceptedResult(System.Uri locationUri, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public sealed partial class AcceptVerbsAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider, Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider
    {
        public AcceptVerbsAttribute(string method) { }
        public AcceptVerbsAttribute(params string[] methods) { }
        public System.Collections.Generic.IEnumerable<string> HttpMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        int? Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get { throw null; } }
        string Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Template { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { get { throw null; } set { } }
        public string Route { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class ActionContextAttribute : System.Attribute
    {
        public ActionContextAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public sealed partial class ActionNameAttribute : System.Attribute
    {
        public ActionNameAttribute(string name) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class ActionResult : Microsoft.AspNetCore.Mvc.IActionResult
    {
        protected ActionResult() { }
        public virtual void ExecuteResult(Microsoft.AspNetCore.Mvc.ActionContext context) { }
        public virtual System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public sealed partial class ActionResult<TValue> : Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult
    {
        public ActionResult(Microsoft.AspNetCore.Mvc.ActionResult result) { }
        public ActionResult(TValue value) { }
        public Microsoft.AspNetCore.Mvc.ActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public TValue Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        Microsoft.AspNetCore.Mvc.IActionResult Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult.Convert() { throw null; }
        public static implicit operator Microsoft.AspNetCore.Mvc.ActionResult<TValue> (Microsoft.AspNetCore.Mvc.ActionResult result) { throw null; }
        public static implicit operator Microsoft.AspNetCore.Mvc.ActionResult<TValue> (TValue value) { throw null; }
    }
    public partial class AntiforgeryValidationFailedResult : Microsoft.AspNetCore.Mvc.BadRequestResult, Microsoft.AspNetCore.Mvc.Core.Infrastructure.IAntiforgeryValidationFailedResult, Microsoft.AspNetCore.Mvc.IActionResult
    {
        public AntiforgeryValidationFailedResult() { }
    }
    public partial class ApiBehaviorOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public ApiBehaviorOptions() { }
        public System.Collections.Generic.IDictionary<int, Microsoft.AspNetCore.Mvc.ClientErrorData> ClientErrorMapping { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.ActionContext, Microsoft.AspNetCore.Mvc.IActionResult> InvalidModelStateResponseFactory { get { throw null; } set { } }
        public bool SuppressConsumesConstraintForFormFileParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressInferBindingSourcesForParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressMapClientErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressModelStateInvalidFilter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class ApiControllerAttribute : Microsoft.AspNetCore.Mvc.ControllerAttribute, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Infrastructure.IApiBehaviorMetadata
    {
        public ApiControllerAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public sealed partial class ApiConventionMethodAttribute : System.Attribute
    {
        public ApiConventionMethodAttribute(System.Type conventionType, string methodName) { }
        public System.Type ConventionType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class ApiConventionTypeAttribute : System.Attribute
    {
        public ApiConventionTypeAttribute(System.Type conventionType) { }
        public System.Type ConventionType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ApiDescriptionActionData
    {
        public ApiDescriptionActionData() { }
        public string GroupName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class ApiExplorerSettingsAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupNameProvider, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionVisibilityProvider
    {
        public ApiExplorerSettingsAttribute() { }
        public string GroupName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IgnoreApi { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class AreaAttribute : Microsoft.AspNetCore.Mvc.Routing.RouteValueAttribute
    {
        public AreaAttribute(string areaName) : base (default(string), default(string)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(400)]
    public partial class BadRequestObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public BadRequestObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) : base (default(object)) { }
        public BadRequestObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(400)]
    public partial class BadRequestResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public BadRequestResult() : base (default(int)) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Parameter, AllowMultiple=false, Inherited=true)]
    public partial class BindAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider
    {
        public BindAttribute(params string[] include) { }
        public string[] Include { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        string Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider.Name { get { throw null; } }
        public string Prefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> PropertyFilter { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class BindPropertiesAttribute : System.Attribute
    {
        public BindPropertiesAttribute() { }
        public bool SupportsGet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class BindPropertyAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IRequestPredicateProvider
    {
        public BindPropertyAttribute() { }
        public System.Type BinderType { get { throw null; } set { } }
        public virtual Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } protected set { } }
        System.Func<Microsoft.AspNetCore.Mvc.ActionContext, bool> Microsoft.AspNetCore.Mvc.ModelBinding.IRequestPredicateProvider.RequestPredicate { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SupportsGet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CacheProfile
    {
        public CacheProfile() { }
        public int? Duration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation? Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? NoStore { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string VaryByHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string[] VaryByQueryKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ChallengeResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public ChallengeResult() { }
        public ChallengeResult(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public ChallengeResult(System.Collections.Generic.IList<string> authenticationSchemes) { }
        public ChallengeResult(System.Collections.Generic.IList<string> authenticationSchemes, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public ChallengeResult(string authenticationScheme) { }
        public ChallengeResult(string authenticationScheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public System.Collections.Generic.IList<string> AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class ClientErrorData
    {
        public ClientErrorData() { }
        public string Link { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Title { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public enum CompatibilityVersion
    {
        [System.ObsoleteAttribute("This CompatibilityVersion value is obsolete. The recommended alternatives are Version_3_0 or later.")]
        Version_2_0 = 0,
        [System.ObsoleteAttribute("This CompatibilityVersion value is obsolete. The recommended alternatives are Version_3_0 or later.")]
        Version_2_1 = 1,
        [System.ObsoleteAttribute("This CompatibilityVersion value is obsolete. The recommended alternatives are Version_3_0 or later.")]
        Version_2_2 = 2,
        Version_3_0 = 3,
        Latest = 2147483647,
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(409)]
    public partial class ConflictObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public ConflictObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) : base (default(object)) { }
        public ConflictObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(409)]
    public partial class ConflictResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public ConflictResult() : base (default(int)) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class ConsumesAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintMetadata, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiRequestMetadataProvider, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter
    {
        public static readonly int ConsumesActionConstraintOrder;
        public ConsumesAttribute(string contentType, params string[] otherContentTypes) { }
        public Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection ContentTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        int Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint.Order { get { throw null; } }
        public bool Accept(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintContext context) { throw null; }
        public void OnResourceExecuted(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext context) { }
        public void OnResourceExecuting(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext context) { }
        public void SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { }
    }
    public partial class ContentResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public ContentResult() { }
        public string Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class ControllerAttribute : System.Attribute
    {
        public ControllerAttribute() { }
    }
    [Microsoft.AspNetCore.Mvc.ControllerAttribute]
    public abstract partial class ControllerBase
    {
        protected ControllerBase() { }
        [Microsoft.AspNetCore.Mvc.ControllerContextAttribute]
        public Microsoft.AspNetCore.Mvc.ControllerContext ControllerContext { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory ModelBinderFactory { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator ObjectValidator { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory ProblemDetailsFactory { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper Url { get { throw null; } set { } }
        public System.Security.Claims.ClaimsPrincipal User { get { throw null; } }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted(string uri) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted(string uri, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted(System.Uri uri) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedResult Accepted(System.Uri uri, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult AcceptedAtRoute([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult AcceptedAtRoute(object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult AcceptedAtRoute(string routeName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.BadRequestResult BadRequest() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ConflictResult Conflict() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ConflictObjectResult Conflict([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ConflictObjectResult Conflict([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType, System.Text.Encoding contentEncoding) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedResult Created(string uri, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedResult Created(System.Uri uri, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtActionResult CreatedAtAction(string actionName, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtActionResult CreatedAtAction(string actionName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtActionResult CreatedAtAction(string actionName, string controllerName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtRouteResult CreatedAtRoute(object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtRouteResult CreatedAtRoute(string routeName, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.CreatedAtRouteResult CreatedAtRoute(string routeName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirect(string localUrl) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanent(string localUrl) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPreserveMethod(string localUrl) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.NoContentResult NoContent() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.NotFoundResult NotFound() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.NotFoundObjectResult NotFound([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.OkResult Ok() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.OkObjectResult Ok([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, bool enableRangeProcessing) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ObjectResult Problem(string detail = null, string instance = null, int? statusCode = default(int?), string title = null, string type = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult Redirect(string url) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanent(string url) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanentPreserveMethod(string url) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPreserveMethod(string url) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanentPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePreserveMethod(string pageName, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string authenticationScheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, string authenticationScheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(params string[] authenticationSchemes) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.StatusCodeResult StatusCode([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultStatusCodeAttribute] int statusCode) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ObjectResult StatusCode([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultStatusCodeAttribute] int statusCode, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual bool TryValidateModel(object model) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual bool TryValidateModel(object model, string prefix) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.UnauthorizedResult Unauthorized() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult Unauthorized([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.UnprocessableEntityResult UnprocessableEntity() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult UnprocessableEntity([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult UnprocessableEntity([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ActionResult ValidationProblem() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ActionResult ValidationProblem([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ActionResult ValidationProblem([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ValidationProblemDetails descriptor) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ActionResult ValidationProblem(string detail = null, string instance = null, int? statusCode = default(int?), string title = null, string type = null, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary = null) { throw null; }
    }
    public partial class ControllerContext : Microsoft.AspNetCore.Mvc.ActionContext
    {
        public ControllerContext() { }
        public ControllerContext(Microsoft.AspNetCore.Mvc.ActionContext context) { }
        public new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor ActionDescriptor { get { throw null; } set { } }
        public virtual System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> ValueProviderFactories { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class ControllerContextAttribute : System.Attribute
    {
        public ControllerContextAttribute() { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(201)]
    public partial class CreatedAtActionResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public CreatedAtActionResult(string actionName, string controllerName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public string ActionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(201)]
    public partial class CreatedAtRouteResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public CreatedAtRouteResult(object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public CreatedAtRouteResult(string routeName, object routeValues, [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(201)]
    public partial class CreatedResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public CreatedResult(string location, object value) : base (default(object)) { }
        public CreatedResult(System.Uri location, object value) : base (default(object)) { }
        public string Location { get { throw null; } set { } }
        public override void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    public static partial class DefaultApiConventions
    {
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(201)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        public static void Create([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Any), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object model) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(200)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Delete([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(204)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Edit([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id, [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Any), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object model) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(200)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Find([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(200)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Get([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(201)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        public static void Post([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Any), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object model) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(204)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Put([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id, [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Any), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object model) { }
        [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Prefix)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(204)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(400)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(404)]
        public static void Update([Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Suffix), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object id, [Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior.Any), Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior.Any)] object model) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class DisableRequestSizeLimitAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public DisableRequestSizeLimitAttribute() { }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class EmptyResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public EmptyResult() { }
        public override void ExecuteResult(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    public partial class FileContentResult : Microsoft.AspNetCore.Mvc.FileResult
    {
        public FileContentResult(byte[] fileContents, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) : base (default(string)) { }
        public FileContentResult(byte[] fileContents, string contentType) : base (default(string)) { }
        public byte[] FileContents { get { throw null; } set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public abstract partial class FileResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        protected FileResult(string contentType) { }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EnableRangeProcessing { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Net.Http.Headers.EntityTagHeaderValue EntityTag { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string FileDownloadName { get { throw null; } set { } }
        public System.DateTimeOffset? LastModified { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class FileStreamResult : Microsoft.AspNetCore.Mvc.FileResult
    {
        public FileStreamResult(System.IO.Stream fileStream, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) : base (default(string)) { }
        public FileStreamResult(System.IO.Stream fileStream, string contentType) : base (default(string)) { }
        public System.IO.Stream FileStream { get { throw null; } set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class ForbidResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public ForbidResult() { }
        public ForbidResult(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public ForbidResult(System.Collections.Generic.IList<string> authenticationSchemes) { }
        public ForbidResult(System.Collections.Generic.IList<string> authenticationSchemes, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public ForbidResult(string authenticationScheme) { }
        public ForbidResult(string authenticationScheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public System.Collections.Generic.IList<string> AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class FormatFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public FormatFilterAttribute() { }
        public bool IsReusable { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class FromBodyAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata
    {
        public FromBodyAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class FromFormAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider
    {
        public FromFormAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class FromHeaderAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider
    {
        public FromHeaderAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class FromQueryAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider
    {
        public FromQueryAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class FromRouteAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider
    {
        public FromRouteAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter, AllowMultiple=false, Inherited=true)]
    public partial class FromServicesAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata
    {
        public FromServicesAttribute() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
    }
    public partial class HttpDeleteAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpDeleteAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpDeleteAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpGetAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpGetAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpGetAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpHeadAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpHeadAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpHeadAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpOptionsAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpOptionsAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpOptionsAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpPatchAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpPatchAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpPatchAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpPostAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpPostAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpPostAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial class HttpPutAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
    {
        public HttpPutAttribute() : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public HttpPutAttribute(string template) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
    }
    public partial interface IDesignTimeMvcBuilderConfiguration
    {
        void ConfigureMvc(Microsoft.Extensions.DependencyInjection.IMvcBuilder builder);
    }
    public partial interface IRequestFormLimitsPolicy : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    public partial interface IRequestSizePolicy : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    public partial class JsonOptions
    {
        public JsonOptions() { }
        public System.Text.Json.JsonSerializerOptions JsonSerializerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class JsonResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public JsonResult(object value) { }
        public JsonResult(object value, object serializerSettings) { }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object SerializerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class LocalRedirectResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public LocalRedirectResult(string localUrl) { }
        public LocalRedirectResult(string localUrl, bool permanent) { }
        public LocalRedirectResult(string localUrl, bool permanent, bool preserveMethod) { }
        public bool Permanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool PreserveMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Url { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public partial class MiddlewareFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public MiddlewareFilterAttribute(System.Type configurationType) { }
        public System.Type ConfigurationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=true)]
    public partial class ModelBinderAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelNameProvider
    {
        public ModelBinderAttribute() { }
        public ModelBinderAttribute(System.Type binderType) { }
        public System.Type BinderType { get { throw null; } set { } }
        public virtual Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } protected set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class ModelMetadataTypeAttribute : System.Attribute
    {
        public ModelMetadataTypeAttribute(System.Type type) { }
        public System.Type MetadataType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class MvcOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public MvcOptions() { }
        public bool AllowEmptyInputInBodyModelBinding { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.CacheProfile> CacheProfiles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> Conventions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EnableEndpointRouting { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.FilterCollection Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Formatters.FormatterMappings FormatterMappings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Formatters.FormatterCollection<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> InputFormatters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int MaxIAsyncEnumerableBufferLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MaxModelBindingCollectionSize { get { throw null; } set { } }
        public int MaxModelBindingRecursionDepth { get { throw null; } set { } }
        public int MaxModelValidationErrors { get { throw null; } set { } }
        public int? MaxValidationDepth { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider> ModelBinderProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider ModelBindingMessageProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> ModelMetadataDetailsProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> ModelValidatorProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Formatters.FormatterCollection<Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter> OutputFormatters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool RequireHttpsPermanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RespectBrowserAcceptHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ReturnHttpNotAcceptable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? SslPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressAsyncSuffixInActionNames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressImplicitRequiredAttributeForNonNullableReferenceTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressInputFormatterBuffering { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressOutputFormatterBuffering { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ValidateComplexTypesIfChildValidationFails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> ValueProviderFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(204)]
    public partial class NoContentResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public NoContentResult() : base (default(int)) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public sealed partial class NonActionAttribute : System.Attribute
    {
        public NonActionAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed partial class NonControllerAttribute : System.Attribute
    {
        public NonControllerAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class NonViewComponentAttribute : System.Attribute
    {
        public NonViewComponentAttribute() { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(404)]
    public partial class NotFoundObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public NotFoundObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(404)]
    public partial class NotFoundResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public NotFoundResult() : base (default(int)) { }
    }
    public partial class ObjectResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public ObjectResult(object value) { }
        public Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection ContentTypes { get { throw null; } set { } }
        public System.Type DeclaredType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Formatters.FormatterCollection<Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter> Formatters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute]
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
        public virtual void OnFormatting(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(200)]
    public partial class OkObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public OkObjectResult(object value) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(200)]
    public partial class OkResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public OkResult() : base (default(int)) { }
    }
    public partial class PhysicalFileResult : Microsoft.AspNetCore.Mvc.FileResult
    {
        public PhysicalFileResult(string fileName, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) : base (default(string)) { }
        public PhysicalFileResult(string fileName, string contentType) : base (default(string)) { }
        public string FileName { get { throw null; } set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class ProblemDetails
    {
        public ProblemDetails() { }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("detail")]
        public string Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Text.Json.Serialization.JsonExtensionDataAttribute]
        public System.Collections.Generic.IDictionary<string, object> Extensions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("instance")]
        public string Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("status")]
        public int? Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("title")]
        public string Title { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("type")]
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class ProducesAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        public ProducesAttribute(string contentType, params string[] additionalContentTypes) { }
        public ProducesAttribute(System.Type type) { }
        public Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection ContentTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int StatusCode { get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public virtual void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
        public void SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public sealed partial class ProducesDefaultResponseTypeAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDefaultResponseMetadataProvider, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public ProducesDefaultResponseTypeAttribute() { }
        public ProducesDefaultResponseTypeAttribute(System.Type type) { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        void Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider.SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public sealed partial class ProducesErrorResponseTypeAttribute : System.Attribute
    {
        public ProducesErrorResponseTypeAttribute(System.Type type) { }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public partial class ProducesResponseTypeAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public ProducesResponseTypeAttribute(int statusCode) { }
        public ProducesResponseTypeAttribute(System.Type type, int statusCode) { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        void Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider.SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { }
    }
    public partial class RedirectResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.ViewFeatures.IKeepTempDataResult
    {
        public RedirectResult(string url) { }
        public RedirectResult(string url, bool permanent) { }
        public RedirectResult(string url, bool permanent, bool preserveMethod) { }
        public bool Permanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool PreserveMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Url { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class RedirectToActionResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.ViewFeatures.IKeepTempDataResult
    {
        public RedirectToActionResult(string actionName, string controllerName, object routeValues) { }
        public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent) { }
        public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, bool preserveMethod) { }
        public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, bool preserveMethod, string fragment) { }
        public RedirectToActionResult(string actionName, string controllerName, object routeValues, bool permanent, string fragment) { }
        public RedirectToActionResult(string actionName, string controllerName, object routeValues, string fragment) { }
        public string ActionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Fragment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Permanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool PreserveMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class RedirectToPageResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.ViewFeatures.IKeepTempDataResult
    {
        public RedirectToPageResult(string pageName) { }
        public RedirectToPageResult(string pageName, object routeValues) { }
        public RedirectToPageResult(string pageName, string pageHandler) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, bool preserveMethod) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, bool preserveMethod, string fragment) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues, bool permanent, string fragment) { }
        public RedirectToPageResult(string pageName, string pageHandler, object routeValues, string fragment) { }
        public string Fragment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Host { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PageHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PageName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Permanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool PreserveMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class RedirectToRouteResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.ViewFeatures.IKeepTempDataResult
    {
        public RedirectToRouteResult(object routeValues) { }
        public RedirectToRouteResult(string routeName, object routeValues) { }
        public RedirectToRouteResult(string routeName, object routeValues, bool permanent) { }
        public RedirectToRouteResult(string routeName, object routeValues, bool permanent, bool preserveMethod) { }
        public RedirectToRouteResult(string routeName, object routeValues, bool permanent, bool preserveMethod, string fragment) { }
        public RedirectToRouteResult(string routeName, object routeValues, bool permanent, string fragment) { }
        public RedirectToRouteResult(string routeName, object routeValues, string fragment) { }
        public string Fragment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Permanent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool PreserveMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper UrlHelper { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class RequestFormLimitsAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public RequestFormLimitsAttribute() { }
        public bool BufferBody { get { throw null; } set { } }
        public long BufferBodyLengthLimit { get { throw null; } set { } }
        public bool IsReusable { get { throw null; } }
        public int KeyLengthLimit { get { throw null; } set { } }
        public int MemoryBufferThreshold { get { throw null; } set { } }
        public long MultipartBodyLengthLimit { get { throw null; } set { } }
        public int MultipartBoundaryLengthLimit { get { throw null; } set { } }
        public int MultipartHeadersCountLimit { get { throw null; } set { } }
        public int MultipartHeadersLengthLimit { get { throw null; } set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int ValueCountLimit { get { throw null; } set { } }
        public int ValueLengthLimit { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class RequestSizeLimitAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public RequestSizeLimitAttribute(long bytes) { }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, Inherited=true, AllowMultiple=false)]
    public partial class RequireHttpsAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public RequireHttpsAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Permanent { get { throw null; } set { } }
        protected virtual void HandleNonHttpsRequest(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext filterContext) { }
        public virtual void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext filterContext) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class ResponseCacheAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ResponseCacheAttribute() { }
        public string CacheProfileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Duration { get { throw null; } set { } }
        public bool IsReusable { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation Location { get { throw null; } set { } }
        public bool NoStore { get { throw null; } set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string VaryByHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string[] VaryByQueryKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
        public Microsoft.AspNetCore.Mvc.CacheProfile GetCacheProfile(Microsoft.AspNetCore.Mvc.MvcOptions options) { throw null; }
    }
    public enum ResponseCacheLocation
    {
        Any = 0,
        Client = 1,
        None = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public partial class RouteAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider
    {
        public RouteAttribute(string template) { }
        int? Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { get { throw null; } set { } }
        public string Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class SerializableError : System.Collections.Generic.Dictionary<string, object>
    {
        public SerializableError() { }
        public SerializableError(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    [System.Diagnostics.DebuggerDisplayAttribute("ServiceFilter: Type={ServiceType} Order={Order}")]
    public partial class ServiceFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ServiceFilterAttribute(System.Type type) { }
        public bool IsReusable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type ServiceType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class SignInResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public SignInResult(string authenticationScheme, System.Security.Claims.ClaimsPrincipal principal) { }
        public SignInResult(string authenticationScheme, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public string AuthenticationScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Security.Claims.ClaimsPrincipal Principal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class SignOutResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public SignOutResult() { }
        public SignOutResult(System.Collections.Generic.IList<string> authenticationSchemes) { }
        public SignOutResult(System.Collections.Generic.IList<string> authenticationSchemes, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public SignOutResult(string authenticationScheme) { }
        public SignOutResult(string authenticationScheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        public System.Collections.Generic.IList<string> AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class StatusCodeResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public StatusCodeResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultStatusCodeAttribute] int statusCode) { }
        int? Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult.StatusCode { get { throw null; } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override void ExecuteResult(Microsoft.AspNetCore.Mvc.ActionContext context) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    [System.Diagnostics.DebuggerDisplayAttribute("TypeFilter: Type={ImplementationType} Order={Order}")]
    public partial class TypeFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public TypeFilterAttribute(System.Type type) { }
        public object[] Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type ImplementationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsReusable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(401)]
    public partial class UnauthorizedObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public UnauthorizedObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object value) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(401)]
    public partial class UnauthorizedResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public UnauthorizedResult() : base (default(int)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(422)]
    public partial class UnprocessableEntityObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public UnprocessableEntityObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) : base (default(object)) { }
        public UnprocessableEntityObjectResult([Microsoft.AspNetCore.Mvc.Infrastructure.ActionResultObjectValueAttribute] object error) : base (default(object)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(422)]
    public partial class UnprocessableEntityResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public UnprocessableEntityResult() : base (default(int)) { }
    }
    [Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute(415)]
    public partial class UnsupportedMediaTypeResult : Microsoft.AspNetCore.Mvc.StatusCodeResult
    {
        public UnsupportedMediaTypeResult() : base (default(int)) { }
    }
    public static partial class UrlHelperExtensions
    {
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, object values) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, string controller) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, string controller, object values) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, string controller, object values, string protocol) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, string controller, object values, string protocol, string host) { throw null; }
        public static string Action(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action, string controller, object values, string protocol, string host, string fragment) { throw null; }
        public static string ActionLink(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string action = null, string controller = null, object values = null, string protocol = null, string host = null, string fragment = null) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, object values) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, string pageHandler) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, string pageHandler, object values) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol, string host) { throw null; }
        public static string Page(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName, string pageHandler, object values, string protocol, string host, string fragment) { throw null; }
        public static string PageLink(this Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, string pageName = null, string pageHandler = null, object values = null, string protocol = null, string host = null, string fragment = null) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, object values) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string routeName) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string routeName, object values) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string routeName, object values, string protocol) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string routeName, object values, string protocol, string host) { throw null; }
        public static string RouteUrl(this Microsoft.AspNetCore.Mvc.IUrlHelper helper, string routeName, object values, string protocol, string host, string fragment) { throw null; }
    }
    public partial class ValidationProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public ValidationProblemDetails() { }
        public ValidationProblemDetails(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { }
        public ValidationProblemDetails(System.Collections.Generic.IDictionary<string, string[]> errors) { }
        [System.Text.Json.Serialization.JsonPropertyNameAttribute("errors")]
        public System.Collections.Generic.IDictionary<string, string[]> Errors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class VirtualFileResult : Microsoft.AspNetCore.Mvc.FileResult
    {
        public VirtualFileResult(string fileName, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) : base (default(string)) { }
        public VirtualFileResult(string fileName, string contentType) : base (default(string)) { }
        public string FileName { get { throw null; } set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public abstract partial class ActionMethodSelectorAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintMetadata
    {
        protected ActionMethodSelectorAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Accept(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintContext context) { throw null; }
        public abstract bool IsValidForRequest(Microsoft.AspNetCore.Routing.RouteContext routeContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action);
    }
    public partial class HttpMethodActionConstraint : Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintMetadata
    {
        public static readonly int HttpMethodConstraintOrder;
        public HttpMethodActionConstraint(System.Collections.Generic.IEnumerable<string> httpMethods) { }
        public System.Collections.Generic.IEnumerable<string> HttpMethods { get { throw null; } }
        public int Order { get { throw null; } }
        public virtual bool Accept(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Method | System.AttributeTargets.Parameter, AllowMultiple=false, Inherited=false)]
    public sealed partial class ApiConventionNameMatchAttribute : System.Attribute
    {
        public ApiConventionNameMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior matchBehavior) { }
        public Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior MatchBehavior { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public enum ApiConventionNameMatchBehavior
    {
        Any = 0,
        Exact = 1,
        Prefix = 2,
        Suffix = 3,
    }
    public sealed partial class ApiConventionResult
    {
        public ApiConventionResult(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider> responseMetadataProviders) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider> ResponseMetadataProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter, AllowMultiple=false, Inherited=false)]
    public sealed partial class ApiConventionTypeMatchAttribute : System.Attribute
    {
        public ApiConventionTypeMatchAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior matchBehavior) { }
        public Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior MatchBehavior { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public enum ApiConventionTypeMatchBehavior
    {
        Any = 0,
        AssignableFrom = 1,
    }
    public partial interface IApiDefaultResponseMetadataProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseMetadataProvider, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    public partial interface IApiDescriptionGroupNameProvider
    {
        string GroupName { get; }
    }
    public partial interface IApiDescriptionVisibilityProvider
    {
        bool IgnoreApi { get; }
    }
    public partial interface IApiRequestFormatMetadataProvider
    {
        System.Collections.Generic.IReadOnlyList<string> GetSupportedContentTypes(string contentType, System.Type objectType);
    }
    public partial interface IApiRequestMetadataProvider : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        void SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes);
    }
    public partial interface IApiResponseMetadataProvider : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        int StatusCode { get; }
        System.Type Type { get; }
        void SetContentTypes(Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes);
    }
    public partial interface IApiResponseTypeMetadataProvider
    {
        System.Collections.Generic.IReadOnlyList<string> GetSupportedContentTypes(string contentType, System.Type objectType);
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DisplayName}")]
    public partial class ActionModel : Microsoft.AspNetCore.Mvc.ApplicationModels.IApiExplorerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IFilterModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public ActionModel(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel other) { }
        public ActionModel(System.Reflection.MethodInfo actionMethod, System.Collections.Generic.IReadOnlyList<object> attributes) { }
        public System.Reflection.MethodInfo ActionMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ActionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApiExplorerModel ApiExplorer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel Controller { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DisplayName { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        string Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Name { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.IOutboundParameterTransformer RouteParameterTransformer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, string> RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel> Selectors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ApiConventionApplicationModelConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public ApiConventionApplicationModelConvention(Microsoft.AspNetCore.Mvc.ProducesErrorResponseTypeAttribute defaultErrorResponseType) { }
        public Microsoft.AspNetCore.Mvc.ProducesErrorResponseTypeAttribute DefaultErrorResponseType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    public partial class ApiExplorerModel
    {
        public ApiExplorerModel() { }
        public ApiExplorerModel(Microsoft.AspNetCore.Mvc.ApplicationModels.ApiExplorerModel other) { }
        public string GroupName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? IsVisible { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ApiVisibilityConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public ApiVisibilityConvention() { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("ApplicationModel: Controllers: {Controllers.Count}, Filters: {Filters.Count}")]
    public partial class ApplicationModel : Microsoft.AspNetCore.Mvc.ApplicationModels.IApiExplorerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IFilterModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public ApplicationModel() { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApiExplorerModel ApiExplorer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel> Controllers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ApplicationModelProviderContext
    {
        public ApplicationModelProviderContext(System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> controllerTypes) { }
        public System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> ControllerTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class AttributeRouteModel
    {
        public AttributeRouteModel() { }
        public AttributeRouteModel(Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel other) { }
        public AttributeRouteModel(Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider templateProvider) { }
        public Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsAbsoluteTemplate { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressLinkGeneration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressPathMatching { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel CombineAttributeRouteModel(Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel left, Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel right) { throw null; }
        public static string CombineTemplates(string prefix, string template) { throw null; }
        public static bool IsOverridePattern(string template) { throw null; }
        public static string ReplaceTokens(string template, System.Collections.Generic.IDictionary<string, string> values) { throw null; }
        public static string ReplaceTokens(string template, System.Collections.Generic.IDictionary<string, string> values, Microsoft.AspNetCore.Routing.IOutboundParameterTransformer routeTokenTransformer) { throw null; }
    }
    public partial class ClientErrorResultFilterConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public ClientErrorResultFilterConvention() { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    public partial class ConsumesConstraintForFormFileParameterConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public ConsumesConstraintForFormFileParameterConvention() { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DisplayName}")]
    public partial class ControllerModel : Microsoft.AspNetCore.Mvc.ApplicationModels.IApiExplorerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IFilterModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public ControllerModel(Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel other) { }
        public ControllerModel(System.Reflection.TypeInfo controllerType, System.Collections.Generic.IReadOnlyList<object> attributes) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel> Actions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApiExplorerModel ApiExplorer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel Application { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PropertyModel> ControllerProperties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Reflection.TypeInfo ControllerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string DisplayName { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        string Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Name { get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<string, string> RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel> Selectors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface IActionModelConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action);
    }
    public partial interface IApiExplorerModel
    {
        Microsoft.AspNetCore.Mvc.ApplicationModels.ApiExplorerModel ApiExplorer { get; set; }
    }
    public partial interface IApplicationModelConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application);
    }
    public partial interface IApplicationModelProvider
    {
        int Order { get; }
        void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context);
        void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context);
    }
    public partial interface IBindingModel
    {
        Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo BindingInfo { get; set; }
    }
    public partial interface ICommonModel : Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        System.Collections.Generic.IReadOnlyList<object> Attributes { get; }
        System.Reflection.MemberInfo MemberInfo { get; }
        string Name { get; }
    }
    public partial interface IControllerModelConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel controller);
    }
    public partial interface IFilterModel
    {
        System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { get; }
    }
    public partial class InferParameterBindingInfoConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public InferParameterBindingInfoConvention(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    public partial class InvalidModelStateFilterConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public InvalidModelStateFilterConvention() { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    public partial interface IParameterModelBaseConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase parameter);
    }
    public partial interface IParameterModelConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel parameter);
    }
    public partial interface IPropertyModel
    {
        System.Collections.Generic.IDictionary<object, object> Properties { get; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("ParameterModel: Name={ParameterName}")]
    public partial class ParameterModel : Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public ParameterModel(Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel other) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public ParameterModel(System.Reflection.ParameterInfo parameterInfo, System.Collections.Generic.IReadOnlyList<object> attributes) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public new System.Collections.Generic.IReadOnlyList<object> Attributes { get { throw null; } }
        public string DisplayName { get { throw null; } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        public System.Reflection.ParameterInfo ParameterInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ParameterName { get { throw null; } set { } }
        public new System.Collections.Generic.IDictionary<object, object> Properties { get { throw null; } }
    }
    public abstract partial class ParameterModelBase : Microsoft.AspNetCore.Mvc.ApplicationModels.IBindingModel
    {
        protected ParameterModelBase(Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase other) { }
        protected ParameterModelBase(System.Type parameterType, System.Collections.Generic.IReadOnlyList<object> attributes) { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo BindingInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public System.Type ParameterType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("PropertyModel: Name={PropertyName}")]
    public partial class PropertyModel : Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase, Microsoft.AspNetCore.Mvc.ApplicationModels.IBindingModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public PropertyModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PropertyModel other) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public PropertyModel(System.Reflection.PropertyInfo propertyInfo, System.Collections.Generic.IReadOnlyList<object> attributes) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public new System.Collections.Generic.IReadOnlyList<object> Attributes { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel Controller { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        public new System.Collections.Generic.IDictionary<object, object> Properties { get { throw null; } }
        public System.Reflection.PropertyInfo PropertyInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string PropertyName { get { throw null; } set { } }
    }
    public partial class RouteTokenTransformerConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        public RouteTokenTransformerConvention(Microsoft.AspNetCore.Routing.IOutboundParameterTransformer parameterTransformer) { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { throw null; }
    }
    public partial class SelectorModel
    {
        public SelectorModel() { }
        public SelectorModel(Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel other) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintMetadata> ActionConstraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel AttributeRouteModel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<object> EndpointMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public abstract partial class ApplicationPart
    {
        protected ApplicationPart() { }
        public abstract string Name { get; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=true)]
    public sealed partial class ApplicationPartAttribute : System.Attribute
    {
        public ApplicationPartAttribute(string assemblyName) { }
        public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class ApplicationPartFactory
    {
        protected ApplicationPartFactory() { }
        public static Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartFactory GetApplicationPartFactory(System.Reflection.Assembly assembly) { throw null; }
        public abstract System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetApplicationParts(System.Reflection.Assembly assembly);
    }
    public partial class ApplicationPartManager
    {
        public ApplicationPartManager() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> ApplicationParts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider> FeatureProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void PopulateFeature<TFeature>(TFeature feature) { }
    }
    public partial class AssemblyPart : Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart, Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationPartTypeProvider
    {
        public AssemblyPart(System.Reflection.Assembly assembly) { }
        public System.Reflection.Assembly Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> Types { get { throw null; } }
    }
    public partial class DefaultApplicationPartFactory : Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartFactory
    {
        public DefaultApplicationPartFactory() { }
        public static Microsoft.AspNetCore.Mvc.ApplicationParts.DefaultApplicationPartFactory Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetApplicationParts(System.Reflection.Assembly assembly) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetDefaultApplicationParts(System.Reflection.Assembly assembly) { throw null; }
    }
    public partial interface IApplicationFeatureProvider
    {
    }
    public partial interface IApplicationFeatureProvider<TFeature> : Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider
    {
        void PopulateFeature(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, TFeature feature);
    }
    public partial interface IApplicationPartTypeProvider
    {
        System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> Types { get; }
    }
    public partial interface ICompilationReferencesProvider
    {
        System.Collections.Generic.IEnumerable<string> GetReferencePaths();
    }
    public partial class NullApplicationPartFactory : Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartFactory
    {
        public NullApplicationPartFactory() { }
        public override System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetApplicationParts(System.Reflection.Assembly assembly) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false)]
    public sealed partial class ProvideApplicationPartFactoryAttribute : System.Attribute
    {
        public ProvideApplicationPartFactoryAttribute(string factoryTypeName) { }
        public ProvideApplicationPartFactoryAttribute(System.Type factoryType) { }
        public System.Type GetFactoryType() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=true)]
    public sealed partial class RelatedAssemblyAttribute : System.Attribute
    {
        public RelatedAssemblyAttribute(string assemblyFileName) { }
        public string AssemblyFileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static System.Collections.Generic.IReadOnlyList<System.Reflection.Assembly> GetRelatedAssemblies(System.Reflection.Assembly assembly, bool throwOnError) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Authorization
{
    public partial class AllowAnonymousFilter : Microsoft.AspNetCore.Mvc.Authorization.IAllowAnonymousFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public AllowAnonymousFilter() { }
    }
    public partial class AuthorizeFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public AuthorizeFilter() { }
        public AuthorizeFilter(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { }
        public AuthorizeFilter(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authorizeData) { }
        public AuthorizeFilter(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authorizeData) { }
        public AuthorizeFilter(string policy) { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> AuthorizeData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        bool Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.IsReusable { get { throw null; } }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicy Policy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider PolicyProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task OnAuthorizationAsync(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Controllers
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DisplayName}")]
    public partial class ControllerActionDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
    {
        public ControllerActionDescriptor() { }
        public virtual string ActionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo ControllerTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override string DisplayName { get { throw null; } set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ControllerActivatorProvider : Microsoft.AspNetCore.Mvc.Controllers.IControllerActivatorProvider
    {
        public ControllerActivatorProvider(Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator controllerActivator) { }
        public System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor) { throw null; }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor) { throw null; }
    }
    public partial class ControllerBoundPropertyDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.IPropertyInfoParameterDescriptor
    {
        public ControllerBoundPropertyDescriptor() { }
        public System.Reflection.PropertyInfo PropertyInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ControllerFeature
    {
        public ControllerFeature() { }
        public System.Collections.Generic.IList<System.Reflection.TypeInfo> Controllers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ControllerFeatureProvider : Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider, Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider<Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature>
    {
        public ControllerFeatureProvider() { }
        protected virtual bool IsController(System.Reflection.TypeInfo typeInfo) { throw null; }
        public void PopulateFeature(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature feature) { }
    }
    public partial class ControllerParameterDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.IParameterInfoParameterDescriptor
    {
        public ControllerParameterDescriptor() { }
        public System.Reflection.ParameterInfo ParameterInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IControllerActivator
    {
        object Create(Microsoft.AspNetCore.Mvc.ControllerContext context);
        void Release(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller);
    }
    public partial interface IControllerActivatorProvider
    {
        System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor);
        System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor);
    }
    public partial interface IControllerFactory
    {
        object CreateController(Microsoft.AspNetCore.Mvc.ControllerContext context);
        void ReleaseController(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller);
    }
    public partial interface IControllerFactoryProvider
    {
        System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateControllerFactory(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor);
        System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateControllerReleaser(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor);
    }
    public partial class ServiceBasedControllerActivator : Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator
    {
        public ServiceBasedControllerActivator() { }
        public object Create(Microsoft.AspNetCore.Mvc.ControllerContext actionContext) { throw null; }
        public virtual void Release(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure
{
    public partial interface IAntiforgeryValidationFailedResult : Microsoft.AspNetCore.Mvc.IActionResult
    {
    }
}
namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed partial class AfterActionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterAction";
        public AfterActionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterActionFilterOnActionExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnActionExecuted";
        public AfterActionFilterOnActionExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext ActionExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterActionFilterOnActionExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnActionExecuting";
        public AfterActionFilterOnActionExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext ActionExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterActionFilterOnActionExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnActionExecution";
        public AfterActionFilterOnActionExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext ActionExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterActionResultEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterActionResult";
        public AfterActionResultEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterAuthorizationFilterOnAuthorizationEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnAuthorization";
        public AfterAuthorizationFilterOnAuthorizationEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext AuthorizationContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterControllerActionMethodEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterControllerActionMethod";
        public AfterControllerActionMethodEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> arguments, object controller, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Controller { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterExceptionFilterOnExceptionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnException";
        public AfterExceptionFilterOnExceptionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ExceptionContext ExceptionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterResourceFilterOnResourceExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResourceExecuted";
        public AfterResourceFilterOnResourceExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext ResourceExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterResourceFilterOnResourceExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResourceExecuting";
        public AfterResourceFilterOnResourceExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext ResourceExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterResourceFilterOnResourceExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResourceExecution";
        public AfterResourceFilterOnResourceExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext ResourceExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterResultFilterOnResultExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResultExecuted";
        public AfterResultFilterOnResultExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext ResultExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterResultFilterOnResultExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResultExecuting";
        public AfterResultFilterOnResultExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext ResultExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterResultFilterOnResultExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnResultExecution";
        public AfterResultFilterOnResultExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext ResultExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeActionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeAction";
        public BeforeActionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeActionFilterOnActionExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnActionExecuted";
        public BeforeActionFilterOnActionExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext ActionExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeActionFilterOnActionExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnActionExecuting";
        public BeforeActionFilterOnActionExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext ActionExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeActionFilterOnActionExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnActionExecution";
        public BeforeActionFilterOnActionExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext ActionExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeActionResultEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeActionResult";
        public BeforeActionResultEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeAuthorizationFilterOnAuthorizationEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnAuthorization";
        public BeforeAuthorizationFilterOnAuthorizationEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext AuthorizationContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeControllerActionMethodEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeControllerActionMethod";
        public BeforeControllerActionMethodEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> actionArguments, object controller) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> ActionArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Controller { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected sealed override int Count { get { throw null; } }
        protected sealed override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeExceptionFilterOnException : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnException";
        public BeforeExceptionFilterOnException(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ExceptionContext ExceptionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeResourceFilterOnResourceExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuted";
        public BeforeResourceFilterOnResourceExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext ResourceExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeResourceFilterOnResourceExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuting";
        public BeforeResourceFilterOnResourceExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext ResourceExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeResourceFilterOnResourceExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecution";
        public BeforeResourceFilterOnResourceExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext ResourceExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeResultFilterOnResultExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResultExecuted";
        public BeforeResultFilterOnResultExecutedEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext ResultExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeResultFilterOnResultExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResultExecuting";
        public BeforeResultFilterOnResultExecutingEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext ResultExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeResultFilterOnResultExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnResultExecution";
        public BeforeResultFilterOnResultExecutionEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext ResultExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class EventData : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        protected const string EventNamespace = "Microsoft.AspNetCore.Mvc.";
        protected EventData() { }
        protected abstract int Count { get; }
        protected abstract System.Collections.Generic.KeyValuePair<string, object> this[int index] { get; }
        int System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Count { get { throw null; } }
        System.Collections.Generic.KeyValuePair<string, object> System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.this[int index] { get { throw null; } }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, object> Current { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public abstract partial class ActionFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        protected ActionFilterAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public virtual void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next) { throw null; }
        public virtual void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public virtual void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task OnResultExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ResultExecutionDelegate next) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public abstract partial class ExceptionFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IAsyncExceptionFilter, Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        protected ExceptionFilterAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context) { }
        public virtual System.Threading.Tasks.Task OnExceptionAsync(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context) { throw null; }
    }
    public partial class FilterCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>
    {
        public FilterCollection() { }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Add(System.Type filterType) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Add(System.Type filterType, int order) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata AddService(System.Type filterType) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata AddService(System.Type filterType, int order) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata AddService<TFilterType>() where TFilterType : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata AddService<TFilterType>(int order) where TFilterType : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Add<TFilterType>() where TFilterType : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata Add<TFilterType>(int order) where TFilterType : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata { throw null; }
    }
    public static partial class FilterScope
    {
        public static readonly int Action;
        public static readonly int Controller;
        public static readonly int First;
        public static readonly int Global;
        public static readonly int Last;
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public abstract partial class ResultFilterAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        protected ResultFilterAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public virtual void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task OnResultExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ResultExecutionDelegate next) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public partial class FormatFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        public FormatFilter(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public virtual string GetFormat(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
        public void OnResourceExecuted(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext context) { }
        public void OnResourceExecuting(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext context) { }
        public void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
    }
    public partial class FormatterMappings
    {
        public FormatterMappings() { }
        public bool ClearMediaTypeMappingForFormat(string format) { throw null; }
        public string GetMediaTypeMappingForFormat(string format) { throw null; }
        public void SetMediaTypeMappingForFormat(string format, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) { }
        public void SetMediaTypeMappingForFormat(string format, string contentType) { }
    }
    public partial class HttpNoContentOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter
    {
        public HttpNoContentOutputFormatter() { }
        public bool TreatNullValueAsNoContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool CanWriteResult(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context) { throw null; }
        public System.Threading.Tasks.Task WriteAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
    }
    public abstract partial class InputFormatter : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiRequestFormatMetadataProvider, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter
    {
        protected InputFormatter() { }
        public Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection SupportedMediaTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual bool CanRead(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
        protected virtual bool CanReadType(System.Type type) { throw null; }
        protected virtual object GetDefaultValueForType(System.Type modelType) { throw null; }
        public virtual System.Collections.Generic.IReadOnlyList<string> GetSupportedContentTypes(string contentType, System.Type objectType) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct MediaType
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MediaType(Microsoft.Extensions.Primitives.StringSegment mediaType) { throw null; }
        public MediaType(string mediaType) { throw null; }
        public MediaType(string mediaType, int offset, int? length) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment Charset { get { throw null; } }
        public System.Text.Encoding Encoding { get { throw null; } }
        public bool HasWildcard { get { throw null; } }
        public bool MatchesAllSubTypes { get { throw null; } }
        public bool MatchesAllSubTypesWithoutSuffix { get { throw null; } }
        public bool MatchesAllTypes { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringSegment SubType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Primitives.StringSegment SubTypeSuffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Primitives.StringSegment SubTypeWithoutSuffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Primitives.StringSegment Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start) { throw null; }
        public static System.Text.Encoding GetEncoding(Microsoft.Extensions.Primitives.StringSegment mediaType) { throw null; }
        public static System.Text.Encoding GetEncoding(string mediaType) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment GetParameter(Microsoft.Extensions.Primitives.StringSegment parameterName) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment GetParameter(string parameterName) { throw null; }
        public bool IsSubsetOf(Microsoft.AspNetCore.Mvc.Formatters.MediaType @set) { throw null; }
        public static string ReplaceEncoding(Microsoft.Extensions.Primitives.StringSegment mediaType, System.Text.Encoding encoding) { throw null; }
        public static string ReplaceEncoding(string mediaType, System.Text.Encoding encoding) { throw null; }
    }
    public partial class MediaTypeCollection : System.Collections.ObjectModel.Collection<string>
    {
        public MediaTypeCollection() { }
        public void Add(Microsoft.Net.Http.Headers.MediaTypeHeaderValue item) { }
        public void Insert(int index, Microsoft.Net.Http.Headers.MediaTypeHeaderValue item) { }
        public bool Remove(Microsoft.Net.Http.Headers.MediaTypeHeaderValue item) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct MediaTypeSegmentWithQuality
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MediaTypeSegmentWithQuality(Microsoft.Extensions.Primitives.StringSegment mediaType, double quality) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment MediaType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public double Quality { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override string ToString() { throw null; }
    }
    public abstract partial class OutputFormatter : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiResponseTypeMetadataProvider, Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter
    {
        protected OutputFormatter() { }
        public Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection SupportedMediaTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual bool CanWriteResult(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context) { throw null; }
        protected virtual bool CanWriteType(System.Type type) { throw null; }
        public virtual System.Collections.Generic.IReadOnlyList<string> GetSupportedContentTypes(string contentType, System.Type objectType) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
        public abstract System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context);
        public virtual void WriteResponseHeaders(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { }
    }
    public partial class StreamOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter
    {
        public StreamOutputFormatter() { }
        public bool CanWriteResult(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task WriteAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
    }
    public partial class StringOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        public StringOutputFormatter() { }
        public override bool CanWriteResult(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context) { throw null; }
        public override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding encoding) { throw null; }
    }
    public partial class SystemTextJsonInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy
    {
        public SystemTextJsonInputFormatter(Microsoft.AspNetCore.Mvc.JsonOptions options, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter> logger) { }
        Microsoft.AspNetCore.Mvc.Formatters.InputFormatterExceptionPolicy Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy.ExceptionPolicy { get { throw null; } }
        public System.Text.Json.JsonSerializerOptions SerializerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public sealed override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding) { throw null; }
    }
    public partial class SystemTextJsonOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        public SystemTextJsonOutputFormatter(System.Text.Json.JsonSerializerOptions jsonSerializerOptions) { }
        public System.Text.Json.JsonSerializerOptions SerializerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public sealed override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding) { throw null; }
    }
    public abstract partial class TextInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.InputFormatter
    {
        protected static readonly System.Text.Encoding UTF16EncodingLittleEndian;
        protected static readonly System.Text.Encoding UTF8EncodingWithoutBOM;
        protected TextInputFormatter() { }
        public System.Collections.Generic.IList<System.Text.Encoding> SupportedEncodings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding);
        protected System.Text.Encoding SelectCharacterEncoding(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
    }
    public abstract partial class TextOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.OutputFormatter
    {
        protected TextOutputFormatter() { }
        public System.Collections.Generic.IList<System.Text.Encoding> SupportedEncodings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Text.Encoding SelectCharacterEncoding(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
        public sealed override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
        public abstract System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding);
    }
}
namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public partial class ActionContextAccessor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor
    {
        public ActionContextAccessor() { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { get { throw null; } set { } }
    }
    public partial class ActionDescriptorCollection
    {
        public ActionDescriptorCollection(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> items, int version) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class ActionDescriptorCollectionProvider : Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider
    {
        protected ActionDescriptorCollectionProvider() { }
        public abstract Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get; }
        public abstract Microsoft.Extensions.Primitives.IChangeToken GetChangeToken();
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public sealed partial class ActionResultObjectValueAttribute : System.Attribute
    {
        public ActionResultObjectValueAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter, AllowMultiple=false, Inherited=false)]
    public sealed partial class ActionResultStatusCodeAttribute : System.Attribute
    {
        public ActionResultStatusCodeAttribute() { }
    }
    public partial class AmbiguousActionException : System.InvalidOperationException
    {
        protected AmbiguousActionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AmbiguousActionException(string message) { }
    }
    public partial class CompatibilitySwitch<TValue> : Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch where TValue : struct
    {
        public CompatibilitySwitch(string name) { }
        public CompatibilitySwitch(string name, TValue initialValue) { }
        public bool IsValueSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        object Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch.Value { get { throw null; } set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public TValue Value { get { throw null; } set { } }
    }
    public abstract partial class ConfigureCompatibilityOptions<TOptions> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>
    {
        protected ConfigureCompatibilityOptions(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Infrastructure.MvcCompatibilityOptions> compatibilityOptions) { }
        protected abstract System.Collections.Generic.IReadOnlyDictionary<string, object> DefaultValues { get; }
        protected Microsoft.AspNetCore.Mvc.CompatibilityVersion Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
    }
    public partial class ContentResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.ContentResult>
    {
        public ContentResultExecutor(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.Infrastructure.ContentResultExecutor> logger, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory httpResponseStreamWriterFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.ContentResult result) { throw null; }
    }
    public partial class DefaultOutputFormatterSelector : Microsoft.AspNetCore.Mvc.Infrastructure.OutputFormatterSelector
    {
        public DefaultOutputFormatterSelector(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public override Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter SelectFormatter(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter> formatters, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed partial class DefaultStatusCodeAttribute : System.Attribute
    {
        public DefaultStatusCodeAttribute(int statusCode) { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class FileContentResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.FileResultExecutorBase, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.FileContentResult>
    {
        public FileContentResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.Extensions.Logging.ILogger)) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.FileContentResult result) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteFileAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.FileContentResult result, Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength) { throw null; }
    }
    public partial class FileResultExecutorBase
    {
        protected const int BufferSize = 65536;
        public FileResultExecutorBase(Microsoft.Extensions.Logging.ILogger logger) { }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected static Microsoft.Extensions.Logging.ILogger CreateLogger<T>(Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
        protected virtual (Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength, bool serveBody) SetHeadersAndLog(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.FileResult result, long? fileLength, bool enableRangeProcessing, System.DateTimeOffset? lastModified = default(System.DateTimeOffset?), Microsoft.Net.Http.Headers.EntityTagHeaderValue etag = null) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected static System.Threading.Tasks.Task WriteFileAsync(Microsoft.AspNetCore.Http.HttpContext context, System.IO.Stream fileStream, Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength) { throw null; }
    }
    public partial class FileStreamResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.FileResultExecutorBase, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.FileStreamResult>
    {
        public FileStreamResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.Extensions.Logging.ILogger)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.FileStreamResult result) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteFileAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.FileStreamResult result, Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength) { throw null; }
    }
    public partial interface IActionContextAccessor
    {
        Microsoft.AspNetCore.Mvc.ActionContext ActionContext { get; set; }
    }
    public partial interface IActionDescriptorChangeProvider
    {
        Microsoft.Extensions.Primitives.IChangeToken GetChangeToken();
    }
    public partial interface IActionDescriptorCollectionProvider
    {
        Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get; }
    }
    public partial interface IActionInvokerFactory
    {
        Microsoft.AspNetCore.Mvc.Abstractions.IActionInvoker CreateInvoker(Microsoft.AspNetCore.Mvc.ActionContext actionContext);
    }
    public partial interface IActionResultExecutor<in TResult> where TResult : Microsoft.AspNetCore.Mvc.IActionResult
    {
        System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, TResult result);
    }
    public partial interface IActionResultTypeMapper
    {
        Microsoft.AspNetCore.Mvc.IActionResult Convert(object value, System.Type returnType);
        System.Type GetResultDataType(System.Type returnType);
    }
    public partial interface IActionSelector
    {
        Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor SelectBestCandidate(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> candidates);
        System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> SelectCandidates(Microsoft.AspNetCore.Routing.RouteContext context);
    }
    public partial interface IApiBehaviorMetadata : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    public partial interface IClientErrorActionResult : Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
    }
    public partial interface IClientErrorFactory
    {
        Microsoft.AspNetCore.Mvc.IActionResult GetClientError(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorActionResult clientError);
    }
    public partial interface ICompatibilitySwitch
    {
        bool IsValueSet { get; }
        string Name { get; }
        object Value { get; set; }
    }
    public partial interface IConvertToActionResult
    {
        Microsoft.AspNetCore.Mvc.IActionResult Convert();
    }
    public partial interface IHttpRequestStreamReaderFactory
    {
        System.IO.TextReader CreateReader(System.IO.Stream stream, System.Text.Encoding encoding);
    }
    public partial interface IHttpResponseStreamWriterFactory
    {
        System.IO.TextWriter CreateWriter(System.IO.Stream stream, System.Text.Encoding encoding);
    }
    public partial interface IParameterInfoParameterDescriptor
    {
        System.Reflection.ParameterInfo ParameterInfo { get; }
    }
    public partial interface IPropertyInfoParameterDescriptor
    {
        System.Reflection.PropertyInfo PropertyInfo { get; }
    }
    public partial interface IStatusCodeActionResult : Microsoft.AspNetCore.Mvc.IActionResult
    {
        int? StatusCode { get; }
    }
    public partial class LocalRedirectResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.LocalRedirectResult>
    {
        public LocalRedirectResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.LocalRedirectResult result) { throw null; }
    }
    public partial class ModelStateInvalidFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ModelStateInvalidFilter(Microsoft.AspNetCore.Mvc.ApiBehaviorOptions apiBehaviorOptions, Microsoft.Extensions.Logging.ILogger logger) { }
        public bool IsReusable { get { throw null; } }
        public int Order { get { throw null; } }
        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
    }
    public partial class MvcCompatibilityOptions
    {
        public MvcCompatibilityOptions() { }
        public Microsoft.AspNetCore.Mvc.CompatibilityVersion CompatibilityVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ObjectResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.ObjectResult>
    {
        [System.ObsoleteAttribute("This constructor is obsolete and will be removed in a future release.")]
        public ObjectResultExecutor(Microsoft.AspNetCore.Mvc.Infrastructure.OutputFormatterSelector formatterSelector, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public ObjectResultExecutor(Microsoft.AspNetCore.Mvc.Infrastructure.OutputFormatterSelector formatterSelector, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        protected Microsoft.AspNetCore.Mvc.Infrastructure.OutputFormatterSelector FormatterSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected System.Func<System.IO.Stream, System.Text.Encoding, System.IO.TextWriter> WriterFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.ObjectResult result) { throw null; }
    }
    public abstract partial class OutputFormatterSelector
    {
        protected OutputFormatterSelector() { }
        public abstract Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter SelectFormatter(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter> formatters, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection mediaTypes);
    }
    public partial class PhysicalFileResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.FileResultExecutorBase, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.PhysicalFileResult>
    {
        public PhysicalFileResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.Extensions.Logging.ILogger)) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.PhysicalFileResult result) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Infrastructure.PhysicalFileResultExecutor.FileMetadata GetFileInfo(string path) { throw null; }
        [System.ObsoleteAttribute("This API is no longer called.")]
        protected virtual System.IO.Stream GetFileStream(string path) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteFileAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.PhysicalFileResult result, Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength) { throw null; }
        protected partial class FileMetadata
        {
            public FileMetadata() { }
            public bool Exists { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
            public System.DateTimeOffset LastModified { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
            public long Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        }
    }
    public abstract partial class ProblemDetailsFactory
    {
        protected ProblemDetailsFactory() { }
        public abstract Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(Microsoft.AspNetCore.Http.HttpContext httpContext, int? statusCode = default(int?), string title = null, string type = null, string detail = null, string instance = null);
        public abstract Microsoft.AspNetCore.Mvc.ValidationProblemDetails CreateValidationProblemDetails(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary, int? statusCode = default(int?), string title = null, string type = null, string detail = null, string instance = null);
    }
    public partial class RedirectResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.RedirectResult>
    {
        public RedirectResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.RedirectResult result) { throw null; }
    }
    public partial class RedirectToActionResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.RedirectToActionResult>
    {
        public RedirectToActionResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.RedirectToActionResult result) { throw null; }
    }
    public partial class RedirectToPageResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.RedirectToPageResult>
    {
        public RedirectToPageResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.RedirectToPageResult result) { throw null; }
    }
    public partial class RedirectToRouteResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.RedirectToRouteResult>
    {
        public RedirectToRouteResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.RedirectToRouteResult result) { throw null; }
    }
    public partial class VirtualFileResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.FileResultExecutorBase, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.VirtualFileResult>
    {
        public VirtualFileResultExecutor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment) : base (default(Microsoft.Extensions.Logging.ILogger)) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.VirtualFileResult result) { throw null; }
        [System.ObsoleteAttribute("This API is no longer called.")]
        protected virtual System.IO.Stream GetFileStream(Microsoft.Extensions.FileProviders.IFileInfo fileInfo) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteFileAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.VirtualFileResult result, Microsoft.Extensions.FileProviders.IFileInfo fileInfo, Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long rangeLength) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public enum BindingBehavior
    {
        Optional = 0,
        Never = 1,
        Required = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class BindingBehaviorAttribute : System.Attribute
    {
        public BindingBehaviorAttribute(Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior behavior) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior Behavior { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class BindingSourceValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public BindingSourceValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource) { }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract bool ContainsPrefix(string prefix);
        public virtual Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource) { throw null; }
        public abstract Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class BindNeverAttribute : Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehaviorAttribute
    {
        public BindNeverAttribute() : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior)) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class BindRequiredAttribute : Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehaviorAttribute
    {
        public BindRequiredAttribute() : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior)) { }
    }
    public partial class CompositeValueProvider : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider>, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IEnumerableValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IKeyRewriterValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public CompositeValueProvider() { }
        public CompositeValueProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider> valueProviders) { }
        public virtual bool ContainsPrefix(string prefix) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.CompositeValueProvider> CreateAsync(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> factories) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.CompositeValueProvider> CreateAsync(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext) { throw null; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter() { throw null; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource) { throw null; }
        public virtual System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
        protected override void InsertItem(int index, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider item) { }
        protected override void SetItem(int index, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider item) { }
    }
    public partial class DefaultModelBindingContext : Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext
    {
        public DefaultModelBindingContext() { }
        public override Microsoft.AspNetCore.Mvc.ActionContext ActionContext { get { throw null; } set { } }
        public override string BinderModelName { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } set { } }
        public override string FieldName { get { throw null; } set { } }
        public override bool IsTopLevelObject { get { throw null; } set { } }
        public override object Model { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ModelMetadata { get { throw null; } set { } }
        public override string ModelName { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider OriginalValueProvider { get { throw null; } set { } }
        public override System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> PropertyFilter { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult Result { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary ValidationState { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider ValueProvider { get { throw null; } set { } }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext CreateBindingContext(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo bindingInfo, string modelName) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext.NestedScope EnterNestedScope() { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext.NestedScope EnterNestedScope(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, string fieldName, string modelName, object model) { throw null; }
        protected override void ExitNestedScope() { }
    }
    public partial class DefaultPropertyFilterProvider<TModel> : Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider where TModel : class
    {
        public DefaultPropertyFilterProvider() { }
        public virtual string Prefix { get { throw null; } }
        public virtual System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> PropertyFilter { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression<System.Func<TModel, object>>> PropertyIncludeExpressions { get { throw null; } }
    }
    public partial class EmptyModelMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadataProvider
    {
        public EmptyModelMetadataProvider() : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider)) { }
    }
    public sealed partial class FormFileValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public FormFileValueProvider(Microsoft.AspNetCore.Http.IFormFileCollection files) { }
        public bool ContainsPrefix(string prefix) { throw null; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public sealed partial class FormFileValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public FormFileValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public partial class FormValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.BindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IEnumerableValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public FormValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, Microsoft.AspNetCore.Http.IFormCollection values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource)) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.PrefixContainer PrefixContainer { get { throw null; } }
        public override bool ContainsPrefix(string prefix) { throw null; }
        public virtual System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public partial class FormValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public FormValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public partial interface IBindingSourceValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource);
    }
    public partial interface ICollectionModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        bool CanCreateInstance(System.Type targetType);
    }
    public partial interface IEnumerableValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix);
    }
    public partial interface IKeyRewriterValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter();
    }
    public partial interface IModelBinderFactory
    {
        Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder CreateBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderFactoryContext context);
    }
    public partial class JQueryFormValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.JQueryValueProvider
    {
        public JQueryFormValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues> values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource), default(System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>), default(System.Globalization.CultureInfo)) { }
    }
    public partial class JQueryFormValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public JQueryFormValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public partial class JQueryQueryStringValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.JQueryValueProvider
    {
        public JQueryQueryStringValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues> values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource), default(System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>), default(System.Globalization.CultureInfo)) { }
    }
    public partial class JQueryQueryStringValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public JQueryQueryStringValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public abstract partial class JQueryValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.BindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IEnumerableValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IKeyRewriterValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        protected JQueryValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues> values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource)) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.PrefixContainer PrefixContainer { get { throw null; } }
        public override bool ContainsPrefix(string prefix) { throw null; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider Filter() { throw null; }
        public System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public partial class ModelAttributes
    {
        internal ModelAttributes() { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> ParameterAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> PropertyAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> TypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes GetAttributesForParameter(System.Reflection.ParameterInfo parameterInfo) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes GetAttributesForParameter(System.Reflection.ParameterInfo parameterInfo, System.Type modelType) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes GetAttributesForProperty(System.Type type, System.Reflection.PropertyInfo property) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes GetAttributesForProperty(System.Type containerType, System.Reflection.PropertyInfo property, System.Type modelType) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes GetAttributesForType(System.Type type) { throw null; }
    }
    public partial class ModelBinderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory
    {
        public ModelBinderFactory(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options, System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder CreateBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderFactoryContext context) { throw null; }
    }
    public partial class ModelBinderFactoryContext
    {
        public ModelBinderFactoryContext() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo BindingInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object CacheToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata Metadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class ModelBinderProviderExtensions
    {
        public static void RemoveType(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider> list, System.Type type) { }
        public static void RemoveType<TModelBinderProvider>(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider> list) where TModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider { }
    }
    public static partial class ModelMetadataProviderExtensions
    {
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForProperty(this Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider provider, System.Type containerType, string propertyName) { throw null; }
    }
    public static partial class ModelNames
    {
        public static string CreateIndexModelName(string parentName, int index) { throw null; }
        public static string CreateIndexModelName(string parentName, string index) { throw null; }
        public static string CreatePropertyModelName(string prefix, string propertyName) { throw null; }
    }
    public abstract partial class ObjectModelValidator : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator
    {
        public ObjectModelValidator(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> validatorProviders) { }
        public abstract Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationVisitor GetValidationVisitor(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider validatorProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidatorCache validatorCache, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState);
        public virtual void Validate(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState, string prefix, object model) { }
        public virtual void Validate(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState, string prefix, object model, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata) { }
    }
    public partial class ParameterBinder
    {
        public ParameterBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator validator, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult> BindModelAsync(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder modelBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object value) { throw null; }
    }
    public partial class PrefixContainer
    {
        public PrefixContainer(System.Collections.Generic.ICollection<string> values) { }
        public bool ContainsPrefix(string prefix) { throw null; }
        public System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix) { throw null; }
    }
    public partial class QueryStringValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.BindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IEnumerableValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public QueryStringValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, Microsoft.AspNetCore.Http.IQueryCollection values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource)) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.PrefixContainer PrefixContainer { get { throw null; } }
        public override bool ContainsPrefix(string prefix) { throw null; }
        public virtual System.Collections.Generic.IDictionary<string, string> GetKeysFromPrefix(string prefix) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public partial class QueryStringValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public QueryStringValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public partial class RouteValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.BindingSourceValueProvider
    {
        public RouteValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, Microsoft.AspNetCore.Routing.RouteValueDictionary values) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource)) { }
        public RouteValueProvider(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource, Microsoft.AspNetCore.Routing.RouteValueDictionary values, System.Globalization.CultureInfo culture) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource)) { }
        protected System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.PrefixContainer PrefixContainer { get { throw null; } }
        public override bool ContainsPrefix(string key) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public partial class RouteValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
    {
        public RouteValueProviderFactory() { }
        public System.Threading.Tasks.Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context) { throw null; }
    }
    public partial class SuppressChildValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public SuppressChildValidationMetadataProvider(string fullTypeName) { }
        public SuppressChildValidationMetadataProvider(System.Type type) { }
        public string FullTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
    public partial class UnsupportedContentTypeException : System.Exception
    {
        public UnsupportedContentTypeException(string message) { }
    }
    public partial class UnsupportedContentTypeFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public UnsupportedContentTypeFilter() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
    }
    public static partial class ValueProviderFactoryExtensions
    {
        public static void RemoveType(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> list, System.Type type) { }
        public static void RemoveType<TValueProviderFactory>(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> list) where TValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public partial class ArrayModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public ArrayModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class ArrayModelBinder<TElement> : Microsoft.AspNetCore.Mvc.ModelBinding.Binders.CollectionModelBinder<TElement>
    {
        public ArrayModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public ArrayModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public ArrayModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public override bool CanCreateInstance(System.Type targetType) { throw null; }
        protected override object ConvertToCollectionType(System.Type targetType, System.Collections.Generic.IEnumerable<TElement> collection) { throw null; }
        protected override void CopyToModel(object target, System.Collections.Generic.IEnumerable<TElement> sourceCollection) { }
        protected override object CreateEmptyCollection(System.Type targetType) { throw null; }
    }
    public partial class BinderTypeModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public BinderTypeModelBinder(System.Type binderType) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class BinderTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public BinderTypeModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class BodyModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public BodyModelBinder(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory) { }
        public BodyModelBinder(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public BodyModelBinder(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class BodyModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public BodyModelBinderProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory) { }
        public BodyModelBinderProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public BodyModelBinderProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter> formatters, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class ByteArrayModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public ByteArrayModelBinder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class ByteArrayModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public ByteArrayModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class CancellationTokenModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public CancellationTokenModelBinder() { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class CancellationTokenModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public CancellationTokenModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class CollectionModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public CollectionModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class CollectionModelBinder<TElement> : Microsoft.AspNetCore.Mvc.ModelBinding.ICollectionModelBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public CollectionModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public CollectionModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes) { }
        public CollectionModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder elementBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder ElementBinder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected void AddErrorIfBindingRequired(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        public virtual bool CanCreateInstance(System.Type targetType) { throw null; }
        protected virtual object ConvertToCollectionType(System.Type targetType, System.Collections.Generic.IEnumerable<TElement> collection) { throw null; }
        protected virtual void CopyToModel(object target, System.Collections.Generic.IEnumerable<TElement> sourceCollection) { }
        protected virtual object CreateEmptyCollection(System.Type targetType) { throw null; }
        protected object CreateInstance(System.Type targetType) { throw null; }
    }
    public partial class ComplexTypeModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public ComplexTypeModelBinder(System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder> propertyBinders, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public ComplexTypeModelBinder(System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder> propertyBinders, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        protected virtual System.Threading.Tasks.Task BindProperty(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        protected virtual bool CanBindProperty(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata propertyMetadata) { throw null; }
        protected virtual object CreateModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        protected virtual void SetProperty(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, string modelName, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata propertyMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult result) { }
    }
    public partial class ComplexTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public ComplexTypeModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class DecimalModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public DecimalModelBinder(System.Globalization.NumberStyles supportedStyles, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class DictionaryModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public DictionaryModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class DictionaryModelBinder<TKey, TValue> : Microsoft.AspNetCore.Mvc.ModelBinding.Binders.CollectionModelBinder<System.Collections.Generic.KeyValuePair<TKey, TValue>>
    {
        public DictionaryModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder keyBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder valueBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public DictionaryModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder keyBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder valueBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public DictionaryModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder keyBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder valueBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        public override bool CanCreateInstance(System.Type targetType) { throw null; }
        protected override object ConvertToCollectionType(System.Type targetType, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection) { throw null; }
        protected override object CreateEmptyCollection(System.Type targetType) { throw null; }
    }
    public partial class DoubleModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public DoubleModelBinder(System.Globalization.NumberStyles supportedStyles, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class EnumTypeModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.Binders.SimpleTypeModelBinder
    {
        public EnumTypeModelBinder(bool suppressBindingUndefinedValueToEnumType, System.Type modelType, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(System.Type), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        protected override void CheckModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult valueProviderResult, object model) { }
    }
    public partial class EnumTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public EnumTypeModelBinderProvider(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class FloatingPointTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public FloatingPointTypeModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class FloatModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public FloatModelBinder(System.Globalization.NumberStyles supportedStyles, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class FormCollectionModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public FormCollectionModelBinder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class FormCollectionModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public FormCollectionModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class FormFileModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public FormFileModelBinder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class FormFileModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public FormFileModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class HeaderModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public HeaderModelBinder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public HeaderModelBinder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder innerModelBinder) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class HeaderModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public HeaderModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class KeyValuePairModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public KeyValuePairModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class KeyValuePairModelBinder<TKey, TValue> : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public KeyValuePairModelBinder(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder keyBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder valueBinder, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class ServicesModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public ServicesModelBinder() { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    public partial class ServicesModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public ServicesModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
    public partial class SimpleTypeModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public SimpleTypeModelBinder(System.Type type, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        protected virtual void CheckModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult valueProviderResult, object model) { }
    }
    public partial class SimpleTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        public SimpleTypeModelBinderProvider() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder GetBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public partial class BindingMetadata
    {
        public BindingMetadata() { }
        public string BinderModelName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type BinderType { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsBindingAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsBindingRequired { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? IsReadOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider ModelBindingMessageProvider { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider PropertyFilterProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class BindingMetadataProviderContext
    {
        public BindingMetadataProviderContext(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key, Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes attributes) { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadata BindingMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> ParameterAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> PropertyAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> TypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class BindingSourceMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        public BindingSourceMetadataProvider(System.Type type, Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
    }
    public partial class DefaultMetadataDetails
    {
        public DefaultMetadataDetails(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key, Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes attributes) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadata BindingMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ContainerMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadata DisplayMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes ModelAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata[] Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<object, object> PropertyGetter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Action<object, object> PropertySetter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadata ValidationMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DefaultModelBindingMessageProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelBindingMessageProvider
    {
        public DefaultModelBindingMessageProvider() { }
        public DefaultModelBindingMessageProvider(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider originalProvider) { }
        public override System.Func<string, string, string> AttemptedValueIsInvalidAccessor { get { throw null; } }
        public override System.Func<string, string> MissingBindRequiredValueAccessor { get { throw null; } }
        public override System.Func<string> MissingKeyOrValueAccessor { get { throw null; } }
        public override System.Func<string> MissingRequestBodyRequiredValueAccessor { get { throw null; } }
        public override System.Func<string, string> NonPropertyAttemptedValueIsInvalidAccessor { get { throw null; } }
        public override System.Func<string> NonPropertyUnknownValueIsInvalidAccessor { get { throw null; } }
        public override System.Func<string> NonPropertyValueMustBeANumberAccessor { get { throw null; } }
        public override System.Func<string, string> UnknownValueIsInvalidAccessor { get { throw null; } }
        public override System.Func<string, string> ValueIsInvalidAccessor { get { throw null; } }
        public override System.Func<string, string> ValueMustBeANumberAccessor { get { throw null; } }
        public override System.Func<string, string> ValueMustNotBeNullAccessor { get { throw null; } }
        public void SetAttemptedValueIsInvalidAccessor(System.Func<string, string, string> attemptedValueIsInvalidAccessor) { }
        public void SetMissingBindRequiredValueAccessor(System.Func<string, string> missingBindRequiredValueAccessor) { }
        public void SetMissingKeyOrValueAccessor(System.Func<string> missingKeyOrValueAccessor) { }
        public void SetMissingRequestBodyRequiredValueAccessor(System.Func<string> missingRequestBodyRequiredValueAccessor) { }
        public void SetNonPropertyAttemptedValueIsInvalidAccessor(System.Func<string, string> nonPropertyAttemptedValueIsInvalidAccessor) { }
        public void SetNonPropertyUnknownValueIsInvalidAccessor(System.Func<string> nonPropertyUnknownValueIsInvalidAccessor) { }
        public void SetNonPropertyValueMustBeANumberAccessor(System.Func<string> nonPropertyValueMustBeANumberAccessor) { }
        public void SetUnknownValueIsInvalidAccessor(System.Func<string, string> unknownValueIsInvalidAccessor) { }
        public void SetValueIsInvalidAccessor(System.Func<string, string> valueIsInvalidAccessor) { }
        public void SetValueMustBeANumberAccessor(System.Func<string, string> valueMustBeANumberAccessor) { }
        public void SetValueMustNotBeNullAccessor(System.Func<string, string> valueMustNotBeNullAccessor) { }
    }
    public partial class DefaultModelMetadata : Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata
    {
        public DefaultModelMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider provider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider detailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails details) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity)) { }
        public DefaultModelMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider provider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider detailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails details, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider modelBindingMessageProvider) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity)) { }
        public override System.Collections.Generic.IReadOnlyDictionary<object, object> AdditionalValues { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes Attributes { get { throw null; } }
        public override string BinderModelName { get { throw null; } }
        public override System.Type BinderType { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadata BindingMetadata { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ContainerMetadata { get { throw null; } }
        public override bool ConvertEmptyStringToNull { get { throw null; } }
        public override string DataTypeName { get { throw null; } }
        public override string Description { get { throw null; } }
        public override string DisplayFormatString { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadata DisplayMetadata { get { throw null; } }
        public override string DisplayName { get { throw null; } }
        public override string EditFormatString { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ElementMetadata { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Microsoft.AspNetCore.Mvc.ModelBinding.EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get { throw null; } }
        public override System.Collections.Generic.IReadOnlyDictionary<string, string> EnumNamesAndValues { get { throw null; } }
        public override bool HasNonDefaultEditFormat { get { throw null; } }
        public override bool? HasValidators { get { throw null; } }
        public override bool HideSurroundingHtml { get { throw null; } }
        public override bool HtmlEncode { get { throw null; } }
        public override bool IsBindingAllowed { get { throw null; } }
        public override bool IsBindingRequired { get { throw null; } }
        public override bool IsEnum { get { throw null; } }
        public override bool IsFlagsEnum { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public override bool IsRequired { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelBindingMessageProvider ModelBindingMessageProvider { get { throw null; } }
        public override string NullDisplayText { get { throw null; } }
        public override int Order { get { throw null; } }
        public override string Placeholder { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelPropertyCollection Properties { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider PropertyFilterProvider { get { throw null; } }
        public override System.Func<object, object> PropertyGetter { get { throw null; } }
        public override System.Action<object, object> PropertySetter { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IPropertyValidationFilter PropertyValidationFilter { get { throw null; } }
        public override bool ShowForDisplay { get { throw null; } }
        public override bool ShowForEdit { get { throw null; } }
        public override string SimpleDisplayProperty { get { throw null; } }
        public override string TemplateHint { get { throw null; } }
        public override bool ValidateChildren { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadata ValidationMetadata { get { throw null; } }
        public override System.Collections.Generic.IReadOnlyList<object> ValidatorMetadata { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata> GetMetadataForProperties(System.Type modelType) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForType(System.Type modelType) { throw null; }
    }
    public partial class DefaultModelMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadataProvider
    {
        public DefaultModelMetadataProvider(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider detailsProvider) { }
        public DefaultModelMetadataProvider(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider detailsProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor) { }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider DetailsProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider ModelBindingMessageProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected virtual Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata CreateModelMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails entry) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails CreateParameterDetails(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails[] CreatePropertyDetails(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultMetadataDetails CreateTypeDetails(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForParameter(System.Reflection.ParameterInfo parameter) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForParameter(System.Reflection.ParameterInfo parameter, System.Type modelType) { throw null; }
        public override System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata> GetMetadataForProperties(System.Type modelType) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForProperty(System.Reflection.PropertyInfo propertyInfo, System.Type modelType) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataForType(System.Type modelType) { throw null; }
    }
    public partial class DisplayMetadata
    {
        public DisplayMetadata() { }
        public System.Collections.Generic.IDictionary<object, object> AdditionalValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool ConvertEmptyStringToNull { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DataTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<string> Description { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DisplayFormatString { get { throw null; } set { } }
        public System.Func<string> DisplayFormatStringProvider { get { throw null; } set { } }
        public System.Func<string> DisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EditFormatString { get { throw null; } set { } }
        public System.Func<string> EditFormatStringProvider { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Microsoft.AspNetCore.Mvc.ModelBinding.EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> EnumNamesAndValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HasNonDefaultEditFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HideSurroundingHtml { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HtmlEncode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsEnum { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsFlagsEnum { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string NullDisplayText { get { throw null; } set { } }
        public System.Func<string> NullDisplayTextProvider { get { throw null; } set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<string> Placeholder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ShowForDisplay { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ShowForEdit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string SimpleDisplayProperty { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string TemplateHint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DisplayMetadataProviderContext
    {
        public DisplayMetadataProviderContext(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key, Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes attributes) { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadata DisplayMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> PropertyAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> TypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ExcludeBindingMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        public ExcludeBindingMetadataProvider(System.Type type) { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
    }
    public partial interface IBindingMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context);
    }
    public partial interface ICompositeMetadataDetailsProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
    }
    public partial interface IDisplayMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context);
    }
    public partial interface IMetadataDetailsProvider
    {
    }
    public partial interface IValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context);
    }
    public static partial class MetadataDetailsProviderExtensions
    {
        public static void RemoveType(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> list, System.Type type) { }
        public static void RemoveType<TMetadataDetailsProvider>(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> list) where TMetadataDetailsProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider { }
    }
    public partial class ValidationMetadata
    {
        public ValidationMetadata() { }
        public bool? HasValidators { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? IsRequired { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IPropertyValidationFilter PropertyValidationFilter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? ValidateChildren { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<object> ValidatorMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ValidationMetadataProviderContext
    {
        public ValidationMetadataProviderContext(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity key, Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes attributes) { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> ParameterAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> PropertyAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> TypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadata ValidationMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public partial class ClientValidatorCache
    {
        public ClientValidatorCache() { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator> GetValidators(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider validatorProvider) { throw null; }
    }
    public partial class CompositeClientModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider
    {
        public CompositeClientModelValidatorProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider> providers) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider> ValidatorProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorProviderContext context) { }
    }
    public partial class CompositeModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider
    {
        public CompositeModelValidatorProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> providers) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> ValidatorProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidatorProviderContext context) { }
    }
    public partial interface IMetadataBasedModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider
    {
        bool HasValidators(System.Type modelType, System.Collections.Generic.IList<object> validatorMetadata);
    }
    public partial interface IObjectModelValidator
    {
        void Validate(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState, string prefix, object model);
    }
    public static partial class ModelValidatorProviderExtensions
    {
        public static void RemoveType(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> list, System.Type type) { }
        public static void RemoveType<TModelValidatorProvider>(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> list) where TModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class ValidateNeverAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IPropertyValidationFilter
    {
        public ValidateNeverAttribute() { }
        public bool ShouldValidateEntry(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry entry, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry parentEntry) { throw null; }
    }
    public partial class ValidationVisitor
    {
        public ValidationVisitor(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider validatorProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidatorCache validatorCache, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState) { }
        [System.ObsoleteAttribute("This property is deprecated and is no longer used by the runtime.")]
        public bool AllowShortCircuitingValidationWhenNoValidatorsArePresent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidatorCache Cache { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected object Container { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Mvc.ActionContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? MaxValidationDepth { get { throw null; } set { } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata Metadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy Strategy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ValidateComplexTypesIfChildValidationFails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary ValidationState { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider ValidatorProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected virtual Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateEntry GetValidationEntry(object model) { throw null; }
        protected virtual void SuppressValidation(string key) { }
        public bool Validate(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
        public virtual bool Validate(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model, bool alwaysValidateAtTopLevel) { throw null; }
        protected virtual bool ValidateNode() { throw null; }
        protected virtual bool Visit(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
        protected virtual bool VisitChildren(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy strategy) { throw null; }
        protected virtual bool VisitComplexType(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy defaultStrategy) { throw null; }
        protected virtual bool VisitSimpleType() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        protected readonly partial struct StateManager : System.IDisposable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public StateManager(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationVisitor visitor, object newModel) { throw null; }
            public void Dispose() { }
            public static Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationVisitor.StateManager Recurse(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationVisitor visitor, string key, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object model, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy strategy) { throw null; }
        }
    }
    public partial class ValidatorCache
    {
        public ValidatorCache() { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidator> GetValidators(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider validatorProvider) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Routing
{
    public abstract partial class DynamicRouteValueTransformer
    {
        protected DynamicRouteValueTransformer() { }
        public abstract System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Routing.RouteValueDictionary> TransformAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteValueDictionary values);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public abstract partial class HttpMethodAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider, Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider
    {
        public HttpMethodAttribute(System.Collections.Generic.IEnumerable<string> httpMethods) { }
        public HttpMethodAttribute(System.Collections.Generic.IEnumerable<string> httpMethods, string template) { }
        public System.Collections.Generic.IEnumerable<string> HttpMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        int? Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { get { throw null; } set { } }
        public string Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface IActionHttpMethodProvider
    {
        System.Collections.Generic.IEnumerable<string> HttpMethods { get; }
    }
    public partial interface IRouteTemplateProvider
    {
        string Name { get; }
        int? Order { get; }
        string Template { get; }
    }
    public partial interface IRouteValueProvider
    {
        string RouteKey { get; }
        string RouteValue { get; }
    }
    public partial interface IUrlHelperFactory
    {
        Microsoft.AspNetCore.Mvc.IUrlHelper GetUrlHelper(Microsoft.AspNetCore.Mvc.ActionContext context);
    }
    public partial class KnownRouteValueConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public KnownRouteValueConstraint(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider) { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public abstract partial class RouteValueAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Routing.IRouteValueProvider
    {
        protected RouteValueAttribute(string routeKey, string routeValue) { }
        public string RouteKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RouteValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class UrlHelper : Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase
    {
        public UrlHelper(Microsoft.AspNetCore.Mvc.ActionContext actionContext) : base (default(Microsoft.AspNetCore.Mvc.ActionContext)) { }
        protected Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        protected Microsoft.AspNetCore.Routing.IRouter Router { get { throw null; } }
        public override string Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) { throw null; }
        protected virtual string GenerateUrl(string protocol, string host, Microsoft.AspNetCore.Routing.VirtualPathData pathData, string fragment) { throw null; }
        protected virtual Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPathData(string routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
        public override string RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) { throw null; }
    }
    public abstract partial class UrlHelperBase : Microsoft.AspNetCore.Mvc.IUrlHelper
    {
        protected UrlHelperBase(Microsoft.AspNetCore.Mvc.ActionContext actionContext) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Routing.RouteValueDictionary AmbientValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract string Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext);
        public virtual string Content(string contentPath) { throw null; }
        protected string GenerateUrl(string protocol, string host, string path) { throw null; }
        protected string GenerateUrl(string protocol, string host, string virtualPath, string fragment) { throw null; }
        protected Microsoft.AspNetCore.Routing.RouteValueDictionary GetValuesDictionary(object values) { throw null; }
        public virtual bool IsLocalUrl(string url) { throw null; }
        public virtual string Link(string routeName, object values) { throw null; }
        public abstract string RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext);
    }
    public partial class UrlHelperFactory : Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory
    {
        public UrlHelperFactory() { }
        public Microsoft.AspNetCore.Mvc.IUrlHelper GetUrlHelper(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public partial interface IKeepTempDataResult : Microsoft.AspNetCore.Mvc.IActionResult
    {
    }
}
namespace Microsoft.AspNetCore.Routing
{
    public static partial class ControllerLinkGeneratorExtensions
    {
        public static string GetPathByAction(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string action = null, string controller = null, object values = null, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetPathByAction(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string action, string controller, object values = null, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByAction(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string action = null, string controller = null, object values = null, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByAction(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string action, string controller, object values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
    }
    public static partial class PageLinkGeneratorExtensions
    {
        public static string GetPathByPage(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string page = null, string handler = null, object values = null, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetPathByPage(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string page, string handler = null, object values = null, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByPage(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string page = null, string handler = null, object values = null, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByPage(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string page, string handler, object values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ApplicationModelConventionExtensions
    {
        public static void Add(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention actionModelConvention) { }
        public static void Add(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.IControllerModelConvention controllerModelConvention) { }
        public static void Add(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.IParameterModelBaseConvention parameterModelConvention) { }
        public static void Add(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.IParameterModelConvention parameterModelConvention) { }
        public static void RemoveType(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> list, System.Type type) { }
        public static void RemoveType<TApplicationModelConvention>(this System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> list) where TApplicationModelConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention { }
    }
    public partial interface IMvcBuilder
    {
        Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get; }
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public partial interface IMvcCoreBuilder
    {
        Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get; }
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public static partial class MvcCoreMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddApplicationPart(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Reflection.Assembly assembly) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddControllersAsServices(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddFormatterMappings(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.FormatterMappings> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddJsonOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.JsonOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder ConfigureApiBehaviorOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder ConfigureApplicationPartManager(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder SetCompatibilityVersion(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, Microsoft.AspNetCore.Mvc.CompatibilityVersion version) { throw null; }
    }
    public static partial class MvcCoreMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddApplicationPart(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Reflection.Assembly assembly) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddAuthorization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddAuthorization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Authorization.AuthorizationOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddControllersAsServices(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddFormatterMappings(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddFormatterMappings(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.FormatterMappings> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddJsonOptions(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.JsonOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcOptions(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder ConfigureApiBehaviorOptions(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder ConfigureApplicationPartManager(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder SetCompatibilityVersion(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, Microsoft.AspNetCore.Mvc.CompatibilityVersion version) { throw null; }
    }
    public static partial class MvcCoreServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Mvc.MvcOptions> setupAction) { throw null; }
    }
}
