// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class AutoValidateAntiforgeryTokenAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public AutoValidateAntiforgeryTokenAttribute() { }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    public abstract partial class Controller : Microsoft.AspNetCore.Mvc.ControllerBase, Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, System.IDisposable
    {
        protected Controller() { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } set { } }
        public dynamic ViewBag { get { throw null; } }
        [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionaryAttribute]
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.JsonResult Json(object data) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.JsonResult Json(object data, object serializerSettings) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual System.Threading.Tasks.Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult PartialView() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult PartialView(object model) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult PartialView(string viewName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult PartialView(string viewName, object model) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewResult View() { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewResult View(object model) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewResult View(string viewName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewResult View(string viewName, object model) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName, object arguments) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType) { throw null; }
        [Microsoft.AspNetCore.Mvc.NonActionAttribute]
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType, object arguments) { throw null; }
    }
    public partial class CookieTempDataProviderOptions
    {
        public CookieTempDataProviderOptions() { }
        public Microsoft.AspNetCore.Http.CookieBuilder Cookie { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class IgnoreAntiforgeryTokenAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.ViewFeatures.IAntiforgeryPolicy
    {
        public IgnoreAntiforgeryTokenAttribute() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IViewComponentHelper
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(string name, object arguments);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(System.Type componentType, object arguments);
    }
    public partial interface IViewComponentResult
    {
        void Execute(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
        System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
    }
    public partial class MvcViewOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public MvcViewOptions() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider> ClientModelValidatorProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions HtmlHelperOptions { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine> ViewEngines { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class PageRemoteAttribute : Microsoft.AspNetCore.Mvc.RemoteAttributeBase
    {
        public PageRemoteAttribute() { }
        public string PageHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PageName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override string GetUrl(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { throw null; }
    }
    public partial class PartialViewResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public PartialViewResult() { }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Model { get { throw null; } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine ViewEngine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class RemoteAttribute : Microsoft.AspNetCore.Mvc.RemoteAttributeBase
    {
        protected RemoteAttribute() { }
        public RemoteAttribute(string routeName) { }
        public RemoteAttribute(string action, string controller) { }
        public RemoteAttribute(string action, string controller, string areaName) { }
        protected string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override string GetUrl(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public abstract partial class RemoteAttributeBase : System.ComponentModel.DataAnnotations.ValidationAttribute, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator
    {
        protected RemoteAttributeBase() { }
        public string AdditionalFields { get { throw null; } set { } }
        public string HttpMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Routing.RouteValueDictionary RouteData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public string FormatAdditionalFieldsForClientValidation(string property) { throw null; }
        public override string FormatErrorMessage(string name) { throw null; }
        public static string FormatPropertyForClientValidation(string property) { throw null; }
        protected abstract string GetUrl(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context);
        public override bool IsValid(object value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class SkipStatusCodePagesAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter
    {
        public SkipStatusCodePagesAttribute() { }
        public void OnResourceExecuted(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext context) { }
        public void OnResourceExecuting(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext context) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, Inherited=true, AllowMultiple=false)]
    public sealed partial class TempDataAttribute : System.Attribute
    {
        public TempDataAttribute() { }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class ValidateAntiForgeryTokenAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ValidateAntiForgeryTokenAttribute() { }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    [Microsoft.AspNetCore.Mvc.ViewComponentAttribute]
    public abstract partial class ViewComponent
    {
        protected ViewComponent() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper Url { get { throw null; } set { } }
        public System.Security.Principal.IPrincipal User { get { throw null; } }
        public System.Security.Claims.ClaimsPrincipal UserClaimsPrincipal { get { throw null; } }
        public dynamic ViewBag { get { throw null; } }
        [Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContextAttribute]
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext ViewComponentContext { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine ViewEngine { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ContentViewComponentResult Content(string content) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult View() { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult View(string viewName) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult View<TModel>(string viewName, TModel model) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult View<TModel>(TModel model) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class ViewComponentAttribute : System.Attribute
    {
        public ViewComponentAttribute() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ViewComponentResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public ViewComponentResult() { }
        public object Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Model { get { throw null; } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewComponentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type ViewComponentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, Inherited=true, AllowMultiple=false)]
    public sealed partial class ViewDataAttribute : System.Attribute
    {
        public ViewDataAttribute() { }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ViewResult : Microsoft.AspNetCore.Mvc.ActionResult, Microsoft.AspNetCore.Mvc.IActionResult, Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult
    {
        public ViewResult() { }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Model { get { throw null; } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine ViewEngine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed partial class AfterViewComponentEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterViewComponent";
        public AfterViewComponentEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext viewComponentContext, Microsoft.AspNetCore.Mvc.IViewComponentResult viewComponentResult, object viewComponent) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public object ViewComponent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext ViewComponentContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IViewComponentResult ViewComponentResult { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterViewEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterView";
        public AfterViewEventData(Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeViewComponentEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeViewComponent";
        public BeforeViewComponentEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext viewComponentContext, object viewComponent) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public object ViewComponent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext ViewComponentContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeViewEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeView";
        public BeforeViewEventData(Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class ViewComponentAfterViewExecuteEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute";
        public ViewComponentAfterViewExecuteEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext viewComponentContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext ViewComponentContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class ViewComponentBeforeViewExecuteEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute";
        public ViewComponentBeforeViewExecuteEventData(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext viewComponentContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext ViewComponentContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class ViewFoundEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.ViewFound";
        public ViewFoundEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, bool isMainPage, Microsoft.AspNetCore.Mvc.ActionResult result, string viewName, Microsoft.AspNetCore.Mvc.ViewEngines.IView view) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public bool IsMainPage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class ViewNotFoundEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.ViewNotFound";
        public ViewNotFoundEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, bool isMainPage, Microsoft.AspNetCore.Mvc.ActionResult result, string viewName, System.Collections.Generic.IEnumerable<string> searchedLocations) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public bool IsMainPage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> SearchedLocations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static partial class ModelStateDictionaryExtensions
    {
        public static void AddModelError<TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Linq.Expressions.Expression<System.Func<TModel, object>> expression, System.Exception exception, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata) { }
        public static void AddModelError<TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Linq.Expressions.Expression<System.Func<TModel, object>> expression, string errorMessage) { }
        public static void RemoveAll<TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Linq.Expressions.Expression<System.Func<TModel, object>> expression) { }
        public static bool Remove<TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Linq.Expressions.Expression<System.Func<TModel, object>> expression) { throw null; }
        public static void TryAddModelException<TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Linq.Expressions.Expression<System.Func<TModel, object>> expression, System.Exception exception) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public enum CheckBoxHiddenInputRenderMode
    {
        None = 0,
        Inline = 1,
        EndOfForm = 2,
    }
    public enum FormMethod
    {
        Get = 0,
        Post = 1,
    }
    public enum Html5DateRenderingMode
    {
        Rfc3339 = 0,
        CurrentCulture = 1,
    }
    public static partial class HtmlHelperComponentExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> RenderComponentAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, System.Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object parameters) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> RenderComponentAsync<TComponent>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> RenderComponentAsync<TComponent>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object parameters) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
    }
    public static partial class HtmlHelperDisplayExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent Display(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Display(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Display(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Display(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Display(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName, string htmlFieldName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, string htmlFieldName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName) { throw null; }
    }
    public static partial class HtmlHelperDisplayNameExtensions
    {
        public static string DisplayNameForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static string DisplayNameFor<TModelItem, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<System.Collections.Generic.IEnumerable<TModelItem>> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModelItem, TResult>> expression) { throw null; }
    }
    public static partial class HtmlHelperEditorExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent Editor(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Editor(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Editor(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Editor(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Editor(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string templateName, string htmlFieldName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, string htmlFieldName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, object additionalViewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName) { throw null; }
    }
    public static partial class HtmlHelperFormExtensions
    {
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, bool? antiforgery) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string actionName, string controllerName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string actionName, string controllerName, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string actionName, string controllerName, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string actionName, string controllerName, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string actionName, string controllerName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object routeValues, bool? antiforgery) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName, bool? antiforgery) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string routeName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
    }
    public static partial class HtmlHelperInputExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent CheckBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent CheckBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, bool isChecked) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent CheckBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent CheckBoxFor<TModel>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, bool>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Hidden(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Hidden(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent HiddenFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Password(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Password(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent PasswordFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RadioButton(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RadioButton(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value, bool isChecked) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RadioButton(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RadioButtonFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextArea(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextArea(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextArea(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextArea(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string value, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextAreaFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextAreaFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object value, string format) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBoxFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBoxFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TextBoxFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string format) { throw null; }
    }
    public static partial class HtmlHelperLabelExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent Label(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Label(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string labelText) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string labelText) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string labelText, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent LabelFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string labelText) { throw null; }
    }
    public static partial class HtmlHelperLinkExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName, object routeValues, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName, string controllerName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName, string controllerName, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ActionLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper helper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RouteLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string linkText, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RouteLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string linkText, object routeValues, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RouteLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string linkText, string routeName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RouteLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string linkText, string routeName, object routeValues) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent RouteLink(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string linkText, string routeName, object routeValues, object htmlAttributes) { throw null; }
    }
    public static partial class HtmlHelperNameExtensions
    {
        public static string IdForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static string NameForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
    }
    public static partial class HtmlHelperPartialExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent Partial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Partial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Partial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent Partial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model) { throw null; }
        public static void RenderPartial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName) { }
        public static void RenderPartial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { }
        public static void RenderPartial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model) { }
        public static void RenderPartial(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { }
        public static System.Threading.Tasks.Task RenderPartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName) { throw null; }
        public static System.Threading.Tasks.Task RenderPartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        public static System.Threading.Tasks.Task RenderPartialAsync(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string partialViewName, object model) { throw null; }
    }
    public static partial class HtmlHelperSelectExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownList(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownList(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownList(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownList(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownList(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string optionLabel) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownListFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownListFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DropDownListFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ListBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ListBox(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ListBoxFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList) { throw null; }
    }
    public static partial class HtmlHelperValidationExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string message) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string message, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression, string message, string tag) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string message) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string message, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string message, string tag) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, bool excludePropertyErrors) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, bool excludePropertyErrors, string message) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, bool excludePropertyErrors, string message, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, bool excludePropertyErrors, string message, string tag) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string message) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string message, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string message, object htmlAttributes, string tag) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string message, string tag) { throw null; }
    }
    public static partial class HtmlHelperValueExtensions
    {
        public static string Value(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string expression) { throw null; }
        public static string ValueForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static string ValueForModel(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string format) { throw null; }
        public static string ValueFor<TModel, TResult>(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> htmlHelper, System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
    }
    public partial interface IHtmlHelper
    {
        Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode Html5DateRenderingMode { get; set; }
        string IdAttributeDotReplacement { get; }
        Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { get; }
        Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get; }
        System.Text.Encodings.Web.UrlEncoder UrlEncoder { get; }
        dynamic ViewBag { get; }
        Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; }
        Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get; }
        Microsoft.AspNetCore.Html.IHtmlContent ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent AntiForgeryToken();
        Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(string actionName, string controllerName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(string routeName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent CheckBox(string expression, bool? isChecked, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent Display(string expression, string templateName, string htmlFieldName, object additionalViewData);
        string DisplayName(string expression);
        string DisplayText(string expression);
        Microsoft.AspNetCore.Html.IHtmlContent DropDownList(string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);
        string Encode(object value);
        string Encode(string value);
        void EndForm();
        string FormatValue(object value, string format);
        string GenerateIdFromName(string fullName);
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumSelectList(System.Type enumType);
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;
        Microsoft.AspNetCore.Html.IHtmlContent Hidden(string expression, object value, object htmlAttributes);
        string Id(string expression);
        Microsoft.AspNetCore.Html.IHtmlContent Label(string expression, string labelText, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent ListBox(string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes);
        string Name(string expression);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData);
        Microsoft.AspNetCore.Html.IHtmlContent Password(string expression, object value, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent RadioButton(string expression, object value, bool? isChecked, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent Raw(object value);
        Microsoft.AspNetCore.Html.IHtmlContent Raw(string value);
        System.Threading.Tasks.Task RenderPartialAsync(string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData);
        Microsoft.AspNetCore.Html.IHtmlContent RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag);
        Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag);
        string Value(string expression, string format);
    }
    public partial interface IHtmlHelper<TModel> : Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper
    {
        new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> ViewData { get; }
        Microsoft.AspNetCore.Html.IHtmlContent CheckBoxFor(System.Linq.Expressions.Expression<System.Func<TModel, bool>> expression, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
        string DisplayNameForInnerType<TModelItem, TResult>(System.Linq.Expressions.Expression<System.Func<TModelItem, TResult>> expression);
        string DisplayNameFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression);
        string DisplayTextFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression);
        Microsoft.AspNetCore.Html.IHtmlContent DropDownListFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
        new string Encode(object value);
        new string Encode(string value);
        Microsoft.AspNetCore.Html.IHtmlContent HiddenFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes);
        string IdFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression);
        Microsoft.AspNetCore.Html.IHtmlContent LabelFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string labelText, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent ListBoxFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes);
        string NameFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression);
        Microsoft.AspNetCore.Html.IHtmlContent PasswordFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent RadioButtonFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object value, object htmlAttributes);
        new Microsoft.AspNetCore.Html.IHtmlContent Raw(object value);
        new Microsoft.AspNetCore.Html.IHtmlContent Raw(string value);
        Microsoft.AspNetCore.Html.IHtmlContent TextAreaFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, int rows, int columns, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent TextBoxFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string format, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string message, object htmlAttributes, string tag);
        string ValueFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string format);
    }
    public partial interface IJsonHelper
    {
        Microsoft.AspNetCore.Html.IHtmlContent Serialize(object value);
    }
    public partial class MultiSelectList : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>, System.Collections.IEnumerable
    {
        public MultiSelectList(System.Collections.IEnumerable items) { }
        public MultiSelectList(System.Collections.IEnumerable items, System.Collections.IEnumerable selectedValues) { }
        public MultiSelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField) { }
        public MultiSelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField, System.Collections.IEnumerable selectedValues) { }
        public MultiSelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField, System.Collections.IEnumerable selectedValues, string dataGroupField) { }
        public string DataGroupField { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string DataTextField { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string DataValueField { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.IEnumerable Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.IEnumerable SelectedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class MvcForm : System.IDisposable
    {
        public MvcForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, System.Text.Encodings.Web.HtmlEncoder htmlEncoder) { }
        public void Dispose() { }
        public void EndForm() { }
        protected virtual void GenerateEndForm() { }
    }
    public enum RenderMode
    {
        Static = 1,
        Server = 2,
        ServerPrerendered = 3,
    }
    public partial class SelectList : Microsoft.AspNetCore.Mvc.Rendering.MultiSelectList
    {
        public SelectList(System.Collections.IEnumerable items) : base (default(System.Collections.IEnumerable)) { }
        public SelectList(System.Collections.IEnumerable items, object selectedValue) : base (default(System.Collections.IEnumerable)) { }
        public SelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField) : base (default(System.Collections.IEnumerable)) { }
        public SelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField, object selectedValue) : base (default(System.Collections.IEnumerable)) { }
        public SelectList(System.Collections.IEnumerable items, string dataValueField, string dataTextField, object selectedValue, string dataGroupField) : base (default(System.Collections.IEnumerable)) { }
        public object SelectedValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class SelectListGroup
    {
        public SelectListGroup() { }
        public bool Disabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class SelectListItem
    {
        public SelectListItem() { }
        public SelectListItem(string text, string value) { }
        public SelectListItem(string text, string value, bool selected) { }
        public SelectListItem(string text, string value, bool selected, bool disabled) { }
        public bool Disabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup Group { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Selected { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Text { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class TagBuilder : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public TagBuilder(string tagName) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.AttributeDictionary Attributes { get { throw null; } }
        public bool HasInnerHtml { get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder InnerHtml { get { throw null; } }
        public string TagName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.TagRenderMode TagRenderMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void AddCssClass(string value) { }
        public static string CreateSanitizedId(string name, string invalidCharReplacement) { throw null; }
        public void GenerateId(string name, string invalidCharReplacement) { }
        public void MergeAttribute(string key, string value) { }
        public void MergeAttribute(string key, string value, bool replaceExisting) { }
        public void MergeAttributes<TKey, TValue>(System.Collections.Generic.IDictionary<TKey, TValue> attributes) { }
        public void MergeAttributes<TKey, TValue>(System.Collections.Generic.IDictionary<TKey, TValue> attributes, bool replaceExisting) { }
        public Microsoft.AspNetCore.Html.IHtmlContent RenderBody() { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RenderEndTag() { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RenderSelfClosingTag() { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RenderStartTag() { throw null; }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public enum TagRenderMode
    {
        Normal = 0,
        StartTag = 1,
        EndTag = 2,
        SelfClosing = 3,
    }
    public static partial class ViewComponentHelperExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(this Microsoft.AspNetCore.Mvc.IViewComponentHelper helper, string name) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(this Microsoft.AspNetCore.Mvc.IViewComponentHelper helper, System.Type componentType) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync<TComponent>(this Microsoft.AspNetCore.Mvc.IViewComponentHelper helper) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync<TComponent>(this Microsoft.AspNetCore.Mvc.IViewComponentHelper helper, object arguments) { throw null; }
    }
    public partial class ViewContext : Microsoft.AspNetCore.Mvc.ActionContext
    {
        public ViewContext() { }
        public ViewContext(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, System.IO.TextWriter writer, Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions htmlHelperOptions) { }
        public ViewContext(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, System.IO.TextWriter writer) { }
        public Microsoft.AspNetCore.Mvc.Rendering.CheckBoxHiddenInputRenderMode CheckBoxHiddenInputRenderMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ClientValidationEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ExecutingFilePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual Microsoft.AspNetCore.Mvc.ViewFeatures.FormContext FormContext { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode Html5DateRenderingMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ValidationMessageElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ValidationSummaryMessageElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public dynamic ViewBag { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.IO.TextWriter Writer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.FormContext GetFormContextForClientValidation() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    public partial class ContentViewComponentResult : Microsoft.AspNetCore.Mvc.IViewComponentResult
    {
        public ContentViewComponentResult(string content) { }
        public string Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void Execute(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { }
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
    }
    public partial class DefaultViewComponentDescriptorCollectionProvider : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorCollectionProvider
    {
        public DefaultViewComponentDescriptorCollectionProvider(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorProvider descriptorProvider) { }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptorCollection ViewComponents { get { throw null; } }
    }
    public partial class DefaultViewComponentDescriptorProvider : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorProvider
    {
        public DefaultViewComponentDescriptorProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager partManager) { }
        protected virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> GetCandidateTypes() { throw null; }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor> GetViewComponents() { throw null; }
    }
    public partial class DefaultViewComponentFactory : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentFactory
    {
        public DefaultViewComponentFactory(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentActivator activator) { }
        public object CreateViewComponent(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
        public void ReleaseViewComponent(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context, object component) { }
    }
    public partial class DefaultViewComponentHelper : Microsoft.AspNetCore.Mvc.IViewComponentHelper, Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware
    {
        public DefaultViewComponentHelper(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorCollectionProvider descriptorProvider, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentSelector selector, Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentInvokerFactory invokerFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope viewBufferScope) { }
        public void Contextualize(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(string name, object arguments) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> InvokeAsync(System.Type componentType, object arguments) { throw null; }
    }
    public partial class DefaultViewComponentSelector : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentSelector
    {
        public DefaultViewComponentSelector(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorCollectionProvider descriptorProvider) { }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor SelectComponent(string componentName) { throw null; }
    }
    public partial class HtmlContentViewComponentResult : Microsoft.AspNetCore.Mvc.IViewComponentResult
    {
        public HtmlContentViewComponentResult(Microsoft.AspNetCore.Html.IHtmlContent encodedContent) { }
        public Microsoft.AspNetCore.Html.IHtmlContent EncodedContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void Execute(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { }
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
    }
    public partial interface IViewComponentActivator
    {
        object Create(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
        void Release(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context, object viewComponent);
    }
    public partial interface IViewComponentDescriptorCollectionProvider
    {
        Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptorCollection ViewComponents { get; }
    }
    public partial interface IViewComponentDescriptorProvider
    {
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor> GetViewComponents();
    }
    public partial interface IViewComponentFactory
    {
        object CreateViewComponent(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
        void ReleaseViewComponent(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context, object component);
    }
    public partial interface IViewComponentInvoker
    {
        System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
    }
    public partial interface IViewComponentInvokerFactory
    {
        Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentInvoker CreateInstance(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context);
    }
    public partial interface IViewComponentSelector
    {
        Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor SelectComponent(string componentName);
    }
    public partial class ServiceBasedViewComponentActivator : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentActivator
    {
        public ServiceBasedViewComponentActivator() { }
        public object Create(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
        public virtual void Release(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context, object viewComponent) { }
    }
    public partial class ViewComponentContext
    {
        public ViewComponentContext() { }
        public ViewComponentContext(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor viewComponentDescriptor, System.Collections.Generic.IDictionary<string, object> arguments, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, System.IO.TextWriter writer) { }
        public System.Collections.Generic.IDictionary<string, object> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor ViewComponentDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } }
        public System.IO.TextWriter Writer { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class ViewComponentContextAttribute : System.Attribute
    {
        public ViewComponentContextAttribute() { }
    }
    public static partial class ViewComponentConventions
    {
        public static readonly string ViewComponentSuffix;
        public static string GetComponentFullName(System.Reflection.TypeInfo componentType) { throw null; }
        public static string GetComponentName(System.Reflection.TypeInfo componentType) { throw null; }
        public static bool IsComponent(System.Reflection.TypeInfo typeInfo) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DisplayName}")]
    public partial class ViewComponentDescriptor
    {
        public ViewComponentDescriptor() { }
        public string DisplayName { get { throw null; } set { } }
        public string FullName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IReadOnlyList<System.Reflection.ParameterInfo> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ShortName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo TypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ViewComponentDescriptorCollection
    {
        public ViewComponentDescriptorCollection(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor> items, int version) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ViewComponentFeature
    {
        public ViewComponentFeature() { }
        public System.Collections.Generic.IList<System.Reflection.TypeInfo> ViewComponents { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ViewComponentFeatureProvider : Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider, Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider<Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentFeature>
    {
        public ViewComponentFeatureProvider() { }
        public void PopulateFeature(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentFeature feature) { }
    }
    public partial class ViewViewComponentResult : Microsoft.AspNetCore.Mvc.IViewComponentResult
    {
        public ViewViewComponentResult() { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine ViewEngine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Execute(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
    public partial class CompositeViewEngine : Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine, Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine
    {
        public CompositeViewEngine(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> optionsAccessor) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine> ViewEngines { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult FindView(Microsoft.AspNetCore.Mvc.ActionContext context, string viewName, bool isMainPage) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage) { throw null; }
    }
    public partial interface ICompositeViewEngine : Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine
    {
        System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine> ViewEngines { get; }
    }
    public partial interface IView
    {
        string Path { get; }
        System.Threading.Tasks.Task RenderAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context);
    }
    public partial interface IViewEngine
    {
        Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult FindView(Microsoft.AspNetCore.Mvc.ActionContext context, string viewName, bool isMainPage);
        Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage);
    }
    public partial class ViewEngineResult
    {
        internal ViewEngineResult() { }
        public System.Collections.Generic.IEnumerable<string> SearchedLocations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Success { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.IView View { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult EnsureSuccessful(System.Collections.Generic.IEnumerable<string> originalLocations) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult Found(string viewName, Microsoft.AspNetCore.Mvc.ViewEngines.IView view) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult NotFound(string viewName, System.Collections.Generic.IEnumerable<string> searchedLocations) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public static partial class AntiforgeryExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent GetHtml(this Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class AttributeDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.Generic.IDictionary<string, string>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.Generic.IReadOnlyDictionary<string, string>, System.Collections.IEnumerable
    {
        public AttributeDictionary() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<System.String,System.String>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<System.String,System.String>.Values { get { throw null; } }
        public System.Collections.Generic.ICollection<string> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, string> item) { }
        public void Add(string key, string value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, string> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, string>[] array, int arrayIndex) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.AttributeDictionary.Enumerator GetEnumerator() { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, string> item) { throw null; }
        public bool Remove(string key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.String>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out string value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public Enumerator(Microsoft.AspNetCore.Mvc.ViewFeatures.AttributeDictionary attributes) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, string> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class CookieTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public static readonly string CookieName;
        public CookieTempDataProvider(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions> options, Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer) { }
        public System.Collections.Generic.IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, System.Collections.Generic.IDictionary<string, object> values) { }
    }
    public partial class DefaultHtmlGenerator : Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator
    {
        public DefaultHtmlGenerator(Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> optionsAccessor, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ViewFeatures.ValidationHtmlAttributeProvider validationAttributeProvider) { }
        protected bool AllowRenderingMaxLengthAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string IdAttributeDotReplacement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected virtual void AddMaxLengthAttribute(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, Microsoft.AspNetCore.Mvc.Rendering.TagBuilder tagBuilder, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression) { }
        protected virtual void AddPlaceholderAttribute(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, Microsoft.AspNetCore.Mvc.Rendering.TagBuilder tagBuilder, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression) { }
        protected virtual void AddValidationAttributes(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.Rendering.TagBuilder tagBuilder, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression) { }
        public string Encode(object value) { throw null; }
        public string Encode(string value) { throw null; }
        public string FormatValue(object value, string format) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateActionLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateAntiforgery(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateCheckBox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, bool? isChecked, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string actionName, string controllerName, object routeValues, string method, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateFormCore(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string action, string method, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent GenerateGroupsAndOptions(string optionLabel, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateHidden(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateHiddenForCheckbox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateInput(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.InputType inputType, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format, System.Collections.Generic.IDictionary<string, object> htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateLabel(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateLink(string linkText, string url, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePageForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePageLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string pageName, string pageHandler, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePassword(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRadioButton(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool? isChecked, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRouteForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string routeName, object routeValues, string method, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRouteLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateSelect(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string optionLabel, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, bool allowMultiple, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateSelect(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string optionLabel, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, System.Collections.Generic.ICollection<string> currentValues, bool allowMultiple, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateTextArea(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateTextBox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateValidationMessage(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateValidationSummary(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, bool excludePropertyErrors, string message, string headerTag, object htmlAttributes) { throw null; }
        public virtual System.Collections.Generic.ICollection<string> GetCurrentValues(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, bool allowMultiple) { throw null; }
    }
    public static partial class DefaultHtmlGeneratorExtensions
    {
        public static Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateForm(this Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator generator, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string actionName, string controllerName, string fragment, object routeValues, string method, object htmlAttributes) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRouteForm(this Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator generator, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string routeName, object routeValues, string fragment, string method, object htmlAttributes) { throw null; }
    }
    public partial class DefaultValidationHtmlAttributeProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ValidationHtmlAttributeProvider
    {
        public DefaultValidationHtmlAttributeProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> optionsAccessor, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorCache clientValidatorCache) { }
        public override void AddValidationAttributes(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, System.Collections.Generic.IDictionary<string, string> attributes) { }
    }
    public partial class FormContext
    {
        public FormContext() { }
        public bool CanRenderAtEndOfForm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Html.IHtmlContent> EndOfFormContent { get { throw null; } }
        public System.Collections.Generic.IDictionary<string, object> FormData { get { throw null; } }
        public bool HasAntiforgeryToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HasEndOfFormContent { get { throw null; } }
        public bool HasFormData { get { throw null; } }
        public bool RenderedField(string fieldName) { throw null; }
        public void RenderedField(string fieldName, bool value) { }
    }
    public partial class HtmlHelper : Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper, Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware
    {
        public static readonly string ValidationInputCssClassName;
        public static readonly string ValidationInputValidCssClassName;
        public static readonly string ValidationMessageCssClassName;
        public static readonly string ValidationMessageValidCssClassName;
        public static readonly string ValidationSummaryCssClassName;
        public static readonly string ValidationSummaryValidCssClassName;
        public HtmlHelper(Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator htmlGenerator, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.Text.Encodings.Web.UrlEncoder urlEncoder) { }
        public Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode Html5DateRenderingMode { get { throw null; } set { } }
        public string IdAttributeDotReplacement { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } }
        public System.Text.Encodings.Web.UrlEncoder UrlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public dynamic ViewBag { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContent ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes) { throw null; }
        public static System.Collections.Generic.IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent AntiForgeryToken() { throw null; }
        public Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginForm(string actionName, string controllerName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Mvc.Rendering.MvcForm BeginRouteForm(string routeName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent CheckBox(string expression, bool? isChecked, object htmlAttributes) { throw null; }
        public virtual void Contextualize(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.MvcForm CreateForm() { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Display(string expression, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        public string DisplayName(string expression) { throw null; }
        public string DisplayText(string expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent DropDownList(string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        public string Encode(object value) { throw null; }
        public string Encode(string value) { throw null; }
        public void EndForm() { }
        public string FormatValue(object value, string format) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateCheckBox(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, bool? isChecked, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateDisplay(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string htmlFieldName, string templateName, object additionalViewData) { throw null; }
        protected virtual string GenerateDisplayName(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression) { throw null; }
        protected virtual string GenerateDisplayText(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { throw null; }
        protected Microsoft.AspNetCore.Html.IHtmlContent GenerateDropDown(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateEditor(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string htmlFieldName, string templateName, object additionalViewData) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.MvcForm GenerateForm(string actionName, string controllerName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateHidden(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes) { throw null; }
        protected virtual string GenerateId(string expression) { throw null; }
        public string GenerateIdFromName(string fullName) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateLabel(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes) { throw null; }
        protected Microsoft.AspNetCore.Html.IHtmlContent GenerateListBox(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes) { throw null; }
        protected virtual string GenerateName(string expression) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GeneratePassword(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateRadioButton(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool? isChecked, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Rendering.MvcForm GenerateRouteForm(string routeName, object routeValues, Microsoft.AspNetCore.Mvc.Rendering.FormMethod method, bool? antiforgery, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateTextArea(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateTextBox(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateValidationMessage(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent GenerateValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag) { throw null; }
        protected virtual string GenerateValue(string expression, object value, string format, bool useViewData) { throw null; }
        protected virtual System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumSelectList(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata) { throw null; }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumSelectList(System.Type enumType) { throw null; }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct { throw null; }
        public static string GetFormMethodString(Microsoft.AspNetCore.Mvc.Rendering.FormMethod method) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Hidden(string expression, object value, object htmlAttributes) { throw null; }
        public string Id(string expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Label(string expression, string labelText, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent ListBox(string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes) { throw null; }
        public string Name(string expression) { throw null; }
        public static System.Collections.Generic.IDictionary<string, object> ObjectToDictionary(object value) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Password(string expression, object value, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RadioButton(string expression, object value, bool? isChecked, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Raw(object value) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Raw(string value) { throw null; }
        public System.Threading.Tasks.Task RenderPartialAsync(string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task RenderPartialCoreAsync(string partialViewName, object model, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, System.IO.TextWriter writer) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag) { throw null; }
        public string Value(string expression, string format) { throw null; }
    }
    public partial class HtmlHelperOptions
    {
        public HtmlHelperOptions() { }
        public Microsoft.AspNetCore.Mvc.Rendering.CheckBoxHiddenInputRenderMode CheckBoxHiddenInputRenderMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ClientValidationEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode Html5DateRenderingMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string IdAttributeDotReplacement { get { throw null; } set { } }
        public string ValidationMessageElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ValidationSummaryMessageElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HtmlHelper<TModel> : Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper, Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper, Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel>
    {
        public HtmlHelper(Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator htmlGenerator, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.Text.Encodings.Web.UrlEncoder urlEncoder, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpressionProvider modelExpressionProvider) : base (default(Microsoft.AspNetCore.Mvc.ViewFeatures.IHtmlGenerator), default(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine), default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope), default(System.Text.Encodings.Web.HtmlEncoder), default(System.Text.Encodings.Web.UrlEncoder)) { }
        public new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContent CheckBoxFor(System.Linq.Expressions.Expression<System.Func<TModel, bool>> expression, object htmlAttributes) { throw null; }
        public override void Contextualize(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        public Microsoft.AspNetCore.Html.IHtmlContent DisplayFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        public string DisplayNameForInnerType<TModelItem, TResult>(System.Linq.Expressions.Expression<System.Func<TModelItem, TResult>> expression) { throw null; }
        public string DisplayNameFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public string DisplayTextFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent DropDownListFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, string optionLabel, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent EditorFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData) { throw null; }
        protected string GetExpressionName<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        protected Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetModelExplorer<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent HiddenFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes) { throw null; }
        public string IdFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent LabelFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string labelText, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent ListBoxFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, object htmlAttributes) { throw null; }
        public string NameFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent PasswordFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent RadioButtonFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, object value, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent TextAreaFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, int rows, int columns, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent TextBoxFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string format, object htmlAttributes) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent ValidationMessageFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string message, object htmlAttributes, string tag) { throw null; }
        public string ValueFor<TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, string format) { throw null; }
    }
    public partial interface IAntiforgeryPolicy : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    public partial interface IFileVersionProvider
    {
        string AddFileVersionToPath(Microsoft.AspNetCore.Http.PathString requestPathBase, string path);
    }
    public partial interface IHtmlGenerator
    {
        string IdAttributeDotReplacement { get; }
        string Encode(object value);
        string Encode(string value);
        string FormatValue(object value, string format);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateActionLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent GenerateAntiforgery(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateCheckBox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, bool? isChecked, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string actionName, string controllerName, object routeValues, string method, object htmlAttributes);
        Microsoft.AspNetCore.Html.IHtmlContent GenerateGroupsAndOptions(string optionLabel, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateHidden(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateHiddenForCheckbox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateLabel(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePageForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePageLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string pageName, string pageHandler, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GeneratePassword(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRadioButton(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, bool? isChecked, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRouteForm(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string routeName, object routeValues, string method, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateRouteLink(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateSelect(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string optionLabel, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, bool allowMultiple, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateSelect(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string optionLabel, string expression, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> selectList, System.Collections.Generic.ICollection<string> currentValues, bool allowMultiple, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateTextArea(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateTextBox(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateValidationMessage(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes);
        Microsoft.AspNetCore.Mvc.Rendering.TagBuilder GenerateValidationSummary(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, bool excludePropertyErrors, string message, string headerTag, object htmlAttributes);
        System.Collections.Generic.ICollection<string> GetCurrentValues(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, bool allowMultiple);
    }
    public partial interface IModelExpressionProvider
    {
        Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression CreateModelExpression<TModel, TValue>(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> viewData, System.Linq.Expressions.Expression<System.Func<TModel, TValue>> expression);
    }
    public enum InputType
    {
        CheckBox = 0,
        Hidden = 1,
        Password = 2,
        Radio = 3,
        Text = 4,
    }
    public partial interface ITempDataDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        void Keep();
        void Keep(string key);
        void Load();
        object Peek(string key);
        void Save();
    }
    public partial interface ITempDataDictionaryFactory
    {
        Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary GetTempData(Microsoft.AspNetCore.Http.HttpContext context);
    }
    public partial interface ITempDataProvider
    {
        System.Collections.Generic.IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context);
        void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, System.Collections.Generic.IDictionary<string, object> values);
    }
    public partial interface IViewContextAware
    {
        void Contextualize(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("DeclaredType={Metadata.ModelType.Name} PropertyName={Metadata.PropertyName}")]
    public partial class ModelExplorer
    {
        public ModelExplorer(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object model) { }
        public ModelExplorer(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer container, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, System.Func<object, object> modelAccessor) { }
        public ModelExplorer(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer container, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object model) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer Container { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata Metadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Model { get { throw null; } }
        public System.Type ModelType { get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer> Properties { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForExpression(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, System.Func<object, object> modelAccessor) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForExpression(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object model) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForExpression(System.Type modelType, System.Func<object, object> modelAccessor) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForExpression(System.Type modelType, object model) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForModel(object model) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForProperty(string name) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForProperty(string name, System.Func<object, object> modelAccessor) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetExplorerForProperty(string name, object model) { throw null; }
    }
    public static partial class ModelExplorerExtensions
    {
        public static string GetSimpleDisplayText(this Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { throw null; }
    }
    public sealed partial class ModelExpression
    {
        public ModelExpression(string name, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata Metadata { get { throw null; } }
        public object Model { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer ModelExplorer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ModelExpressionProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider
    {
        public ModelExpressionProvider(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression CreateModelExpression<TModel>(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> viewData, string expression) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression CreateModelExpression<TModel, TValue>(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> viewData, System.Linq.Expressions.Expression<System.Func<TModel, TValue>> expression) { throw null; }
        public string GetExpressionText<TModel, TValue>(System.Linq.Expressions.Expression<System.Func<TModel, TValue>> expression) { throw null; }
    }
    public static partial class ModelMetadataProviderExtensions
    {
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer GetModelExplorerForType(this Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider provider, System.Type modelType, object model) { throw null; }
    }
    public partial class PartialViewResultExecutor : Microsoft.AspNetCore.Mvc.ViewFeatures.ViewExecutor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.PartialViewResult>
    {
        public PartialViewResultExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> viewOptions, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) : base (default(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions>), default(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory), default(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine), default(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory), default(System.Diagnostics.DiagnosticListener), default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider)) { }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.PartialViewResult result) { throw null; }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.PartialViewResult viewResult) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult FindView(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.PartialViewResult viewResult) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class SaveTempDataAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public SaveTempDataAttribute() { }
        public bool IsReusable { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    public partial class SessionStateTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public SessionStateTempDataProvider(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer) { }
        public virtual System.Collections.Generic.IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public virtual void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, System.Collections.Generic.IDictionary<string, object> values) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class StringHtmlContent : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public StringHtmlContent(string input) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial class TempDataDictionary : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public TempDataDictionary(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider provider) { }
        public int Count { get { throw null; } }
        public object this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.IsReadOnly { get { throw null; } }
        public System.Collections.Generic.ICollection<object> Values { get { throw null; } }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool ContainsKey(string key) { throw null; }
        public bool ContainsValue(object value) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { throw null; }
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public object Peek(string key) { throw null; }
        public bool Remove(string key) { throw null; }
        public void Save() { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Add(System.Collections.Generic.KeyValuePair<string, object> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> keyValuePair) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> keyValuePair) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out object value) { throw null; }
    }
    public partial class TempDataDictionaryFactory : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory
    {
        public TempDataDictionaryFactory(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider provider) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary GetTempData(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class TemplateInfo
    {
        public TemplateInfo() { }
        public TemplateInfo(Microsoft.AspNetCore.Mvc.ViewFeatures.TemplateInfo original) { }
        public object FormattedModelValue { get { throw null; } set { } }
        public string HtmlFieldPrefix { get { throw null; } set { } }
        public int TemplateDepth { get { throw null; } }
        public bool AddVisited(object value) { throw null; }
        public string GetFullHtmlFieldName(string partialFieldName) { throw null; }
        public bool Visited(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { throw null; }
    }
    public delegate bool TryGetValueDelegate(object dictionary, string key, out object value);
    public static partial class TryGetValueProvider
    {
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.TryGetValueDelegate CreateInstance(System.Type targetType) { throw null; }
    }
    public abstract partial class ValidationHtmlAttributeProvider
    {
        protected ValidationHtmlAttributeProvider() { }
        public virtual void AddAndTrackValidationAttributes(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, string expression, System.Collections.Generic.IDictionary<string, string> attributes) { }
        public abstract void AddValidationAttributes(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer, System.Collections.Generic.IDictionary<string, string> attributes);
    }
    public partial class ViewComponentResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.ViewComponentResult>
    {
        [System.ObsoleteAttribute("This constructor is obsolete and will be removed in a future version.")]
        public ViewComponentResultExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcHelperOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataDictionaryFactory) { }
        public ViewComponentResultExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcHelperOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataDictionaryFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.ViewComponentResult result) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class ViewContextAttribute : System.Attribute
    {
        public ViewContextAttribute() { }
    }
    public partial class ViewDataDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { }
        protected ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, System.Type declaredModelType) { }
        protected ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, System.Type declaredModelType) { }
        public ViewDataDictionary(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary source) { }
        protected ViewDataDictionary(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary source, object model, System.Type declaredModelType) { }
        protected ViewDataDictionary(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary source, System.Type declaredModelType) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[string index] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public object Model { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer ModelExplorer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ModelMetadata { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.TemplateInfo TemplateInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.ICollection<object> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        public object Eval(string expression) { throw null; }
        public string Eval(string expression, string format) { throw null; }
        public static string FormatValue(object value, string format) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataInfo GetViewDataInfo(string expression) { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool Remove(string key) { throw null; }
        protected virtual void SetModel(object value) { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out object value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class ViewDataDictionaryAttribute : System.Attribute
    {
        public ViewDataDictionaryAttribute() { }
    }
    public partial class ViewDataDictionaryControllerPropertyActivator
    {
        public ViewDataDictionaryControllerPropertyActivator(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public void Activate(Microsoft.AspNetCore.Mvc.ControllerContext actionContext, object controller) { }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> GetActivatorDelegate(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor) { throw null; }
    }
    public partial class ViewDataDictionary<TModel> : Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary
    {
        public ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)) { }
        public ViewDataDictionary(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary source) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)) { }
        public ViewDataDictionary(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary source, object model) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)) { }
        public new TModel Model { get { throw null; } set { } }
    }
    public static partial class ViewDataEvaluator
    {
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataInfo Eval(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, string expression) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataInfo Eval(object indexableObject, string expression) { throw null; }
    }
    public partial class ViewDataInfo
    {
        public ViewDataInfo(object container, object value) { }
        public ViewDataInfo(object container, System.Reflection.PropertyInfo propertyInfo) { }
        public ViewDataInfo(object container, System.Reflection.PropertyInfo propertyInfo, System.Func<object> valueAccessor) { }
        public object Container { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Reflection.PropertyInfo PropertyInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Value { get { throw null; } set { } }
    }
    public partial class ViewExecutor
    {
        public static readonly string DefaultContentType;
        protected ViewExecutor(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, System.Diagnostics.DiagnosticListener diagnosticListener) { }
        public ViewExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> viewOptions, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        protected System.Diagnostics.DiagnosticListener DiagnosticListener { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider ModelMetadataProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory TempDataFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine ViewEngine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.MvcViewOptions ViewOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory WriterFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ViewEngines.IView view, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, string contentType, int? statusCode) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string contentType, int? statusCode) { throw null; }
    }
    public partial class ViewResultExecutor : Microsoft.AspNetCore.Mvc.ViewFeatures.ViewExecutor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.ViewResult>
    {
        public ViewResultExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> viewOptions, Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) : base (default(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions>), default(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory), default(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine), default(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory), default(System.Diagnostics.DiagnosticListener), default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider)) { }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.ViewResult result) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult FindView(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ViewResult viewResult) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    public partial interface IViewBufferScope
    {
        System.IO.TextWriter CreateWriter(System.IO.TextWriter writer);
        Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] GetPage(int pageSize);
        void ReturnSegment(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] segment);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ViewBufferValue
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ViewBufferValue(Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        public ViewBufferValue(string value) { throw null; }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    public abstract partial class TempDataSerializer
    {
        protected TempDataSerializer() { }
        public virtual bool CanSerializeType(System.Type type) { throw null; }
        public abstract System.Collections.Generic.IDictionary<string, object> Deserialize(byte[] unprotectedData);
        public abstract byte[] Serialize(System.Collections.Generic.IDictionary<string, object> values);
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcViewFeaturesMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddCookieTempDataProvider(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddCookieTempDataProvider(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddSessionStateTempDataProvider(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewComponentsAsServices(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcViewOptions> setupAction) { throw null; }
    }
    public static partial class MvcViewFeaturesMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddCookieTempDataProvider(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddCookieTempDataProvider(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViews(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViews(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcViewOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder ConfigureViews(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcViewOptions> setupAction) { throw null; }
    }
}
