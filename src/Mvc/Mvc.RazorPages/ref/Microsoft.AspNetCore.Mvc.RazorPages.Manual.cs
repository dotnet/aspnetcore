// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal partial class CompiledPageRouteModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelProvider
    {
        public CompiledPageRouteModelProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptionsAccessor, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.ApplicationModels.CompiledPageRouteModelProvider> logger) { }
        public int Order { get { throw null; } }
        internal static string GetRouteTemplate(Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor viewDescriptor) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Razor.Compilation.ViewsFeature GetViewFeature(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context) { }
    }
    internal partial class DefaultPageApplicationModelPartsProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelPartsProvider
    {
        public DefaultPageApplicationModelPartsProvider(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel CreateHandlerModel(System.Reflection.MethodInfo method) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageParameterModel CreateParameterModel(System.Reflection.ParameterInfo parameter) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PagePropertyModel CreatePropertyModel(System.Reflection.PropertyInfo property) { throw null; }
        public bool IsHandler(System.Reflection.MethodInfo methodInfo) { throw null; }
        internal static bool TryParseHandlerMethod(string methodName, out string httpMethod, out string handler) { throw null; }
    }
    public partial class PageConventionCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention>
    {
        internal PageConventionCollection(System.IServiceProvider serviceProvider) { }
        internal Microsoft.AspNetCore.Mvc.MvcOptions MvcOptions { get { throw null; } }
        internal static void EnsureValidFolderPath(string folderPath) { }
        internal static void EnsureValidPageName(string pageName, string argumentName = "pageName") { }
        internal static bool PathBelongsToFolder(string folderPath, string viewEnginePath) { throw null; }
    }
    internal static partial class CompiledPageActionDescriptorBuilder
    {
        public static Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor Build(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel applicationModel, Microsoft.AspNetCore.Mvc.Filters.FilterCollection globalFilters) { throw null; }
        internal static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageBoundPropertyDescriptor[] CreateBoundProperties(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel applicationModel) { throw null; }
        internal static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor[] CreateHandlerMethods(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel applicationModel) { throw null; }
        internal static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerParameterDescriptor[] CreateHandlerParameters(Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel handlerModel) { throw null; }
    }
    internal partial class AutoValidateAntiforgeryPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public AutoValidateAntiforgeryPageApplicationModelProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    // https://github.com/dotnet/arcade/issues/2066
    [System.Diagnostics.DebuggerDisplayAttribute("PageParameterModel: Name={ParameterName}")]
    public partial class PageParameterModel : Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase, Microsoft.AspNetCore.Mvc.ApplicationModels.IBindingModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public PageParameterModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PageParameterModel other) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public PageParameterModel(System.Reflection.ParameterInfo parameterInfo, System.Collections.Generic.IReadOnlyList<object> attributes) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        public System.Reflection.ParameterInfo ParameterInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ParameterName { get { throw null; } set { } }
        System.Collections.Generic.IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get { throw null; } }
        System.Collections.Generic.IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("PagePropertyModel: Name={PropertyName}")]
    public partial class PagePropertyModel : Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModelBase, Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public PagePropertyModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PagePropertyModel other) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        public PagePropertyModel(System.Reflection.PropertyInfo propertyInfo, System.Collections.Generic.IReadOnlyList<object> attributes) : base (default(System.Type), default(System.Collections.Generic.IReadOnlyList<object>)) { }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Reflection.PropertyInfo PropertyInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string PropertyName { get { throw null; } set { } }
        System.Collections.Generic.IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get { throw null; } }
        System.Collections.Generic.IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get { throw null; } }
    }
    internal partial class PageRouteModelFactory
    {
        public PageRouteModelFactory(Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions options, Microsoft.Extensions.Logging.ILogger logger) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel CreateAreaRouteModel(string relativePath, string routeTemplate) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel CreateRouteModel(string relativePath, string routeTemplate) { throw null; }
        internal bool TryParseAreaPath(string relativePath, out (string areaName, string viewEnginePath) result) { throw null; }
    }
    internal partial class AuthorizationPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public AuthorizationPageApplicationModelProvider(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    internal partial class DefaultPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public DefaultPageApplicationModelProvider(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> razorPagesOptions, Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelPartsProvider pageApplicationModelPartsProvider) { }
        public int Order { get { throw null; } }
        protected virtual Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel CreateModel(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor, System.Reflection.TypeInfo pageTypeInfo) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        internal void PopulateFilters(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel pageModel) { }
        internal void PopulateHandlerMethods(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel pageModel) { }
        internal void PopulateHandlerProperties(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel pageModel) { }
    }
    internal partial class TempDataFilterPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public TempDataFilterPageApplicationModelProvider(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    internal partial class ViewDataAttributePageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public ViewDataAttributePageApplicationModelProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    internal partial class ResponseCacheFilterApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        public ResponseCacheFilterApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal partial class PageViewDataAttributeFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public PageViewDataAttributeFilterFactory(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public bool IsReusable { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class PageResponseCacheFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IPageFilter, Microsoft.AspNetCore.Mvc.Filters.IResponseCacheFilter
    {
        public PageResponseCacheFilter(Microsoft.AspNetCore.Mvc.CacheProfile cacheProfile, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public int Duration { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation Location { get { throw null; } set { } }
        public bool NoStore { get { throw null; } set { } }
        public string VaryByHeader { get { throw null; } set { } }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
        public void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        public void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
    }
    internal partial class PageViewDataAttributeFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IPageFilter, Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.IViewDataValuesProviderFeature
    {
        public PageViewDataAttributeFilter(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Subject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        public void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
        public void ProvideViewDataValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { }
    }
    internal partial class PageSaveTempDataPropertyFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public PageSaveTempDataPropertyFilterFactory(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public bool IsReusable { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class PageSaveTempDataPropertyFilter : Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.SaveTempDataPropertyFilterBase, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IPageFilter
    {
        public PageSaveTempDataPropertyFilter(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory factory) : base (default(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)) { }
        public void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        public void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
    }
    internal partial class PageHandlerPageFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public PageHandlerPageFilter() { }
        public int Order { get { throw null; } }
        public System.Threading.Tasks.Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next) { throw null; }
        public System.Threading.Tasks.Task OnPageHandlerSelectionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { throw null; }
    }
    internal partial class PageHandlerResultFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public PageHandlerResultFilter() { }
        public int Order { get { throw null; } }
        public System.Threading.Tasks.Task OnResultExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ResultExecutionDelegate next) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal partial class DynamicPageRouteValueTransformerMetadata : Microsoft.AspNetCore.Routing.IDynamicEndpointMetadata
    {
        public DynamicPageRouteValueTransformerMetadata(System.Type selectorType) { }
        public bool IsDynamic { get { throw null; } }
        public System.Type SelectorType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class DefaultPageModelActivatorProvider : Microsoft.AspNetCore.Mvc.RazorPages.IPageModelActivatorProvider
    {
        public DefaultPageModelActivatorProvider() { }
        public virtual System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
        public virtual System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
    }
    internal partial class PageLoaderMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy
    {
        public PageLoaderMatcherPolicy(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader) { }
        public override int Order { get { throw null; } }
        public bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
    }
    internal partial class DefaultPageActivatorProvider : Microsoft.AspNetCore.Mvc.RazorPages.IPageActivatorProvider
    {
        public DefaultPageActivatorProvider() { }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
    }
    internal partial class DynamicPageEndpointMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy
    {
        public DynamicPageEndpointMatcherPolicy(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.DynamicPageEndpointSelector selector, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader, Microsoft.AspNetCore.Routing.Matching.EndpointMetadataComparer comparer) { }
        public override int Order { get { throw null; } }
        public bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
    }
    internal partial class PageActionInvoker : Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker, Microsoft.AspNetCore.Mvc.Abstractions.IActionInvoker
    {
        public PageActionInvoker(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector handlerMethodSelector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filterMetadata, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions htmlHelperOptions) : base (default(System.Diagnostics.DiagnosticListener), default(Microsoft.Extensions.Logging.ILogger), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper), default(Microsoft.AspNetCore.Mvc.ActionContext), default(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[]), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>)) { }
        internal Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry CacheEntry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.Mvc.RazorPages.PageContext PageContext { get { throw null; } }
        protected override System.Threading.Tasks.Task InvokeInnerFilterAsync() { throw null; }
        protected override System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw null; }
        protected override void ReleaseResources() { }
    }
    internal static partial class ExecutorFactory
    {
        public static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerExecutorDelegate CreateExecutor(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handlerDescriptor) { throw null; }
    }
    internal partial class DefaultPageLoader : Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader
    {
        public DefaultPageLoader(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider> applicationModelProviders, Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider viewCompilerProvider, Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory endpointFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pageOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        internal static void ApplyConventions(Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel pageApplicationModel) { }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor> LoadAsync(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor) { throw null; }
    }
    internal partial class PageActionEndpointDataSource : Microsoft.AspNetCore.Mvc.Routing.ActionEndpointDataSourceBase
    {
        public PageActionEndpointDataSource(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actions, Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory endpointFactory) : base (default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)) { }
        public bool CreateInertEndpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Builder.PageActionEndpointConventionBuilder DefaultBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected override System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> CreateEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { throw null; }
    }
    internal partial class DynamicPageEndpointSelector : System.IDisposable
    {
        public DynamicPageEndpointSelector(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionEndpointDataSource dataSource) { }
        protected DynamicPageEndpointSelector(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        public void Dispose() { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> SelectEndpoints(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    internal partial class DefaultPageModelFactoryProvider : Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider
    {
        public DefaultPageModelFactoryProvider(Microsoft.AspNetCore.Mvc.RazorPages.IPageModelActivatorProvider modelActivator) { }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateModelDisposer(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateModelFactory(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
    }
    internal partial class DefaultPageFactoryProvider : Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider
    {
        public DefaultPageFactoryProvider(Microsoft.AspNetCore.Mvc.RazorPages.IPageActivatorProvider pageActivator, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory, Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper jsonHelper, System.Diagnostics.DiagnosticListener diagnosticListener, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider modelExpressionProvider) { }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreatePageDisposer(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreatePageFactory(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
    }
    internal partial class DefaultPageHandlerMethodSelector : Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector
    {
        public DefaultPageHandlerMethodSelector() { }
        public Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor Select(Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { throw null; }
    }
    internal sealed partial class HandleOptionsRequestsPageFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IPageFilter
    {
        public HandleOptionsRequestsPageFilter() { }
        public int Order { get { throw null; } }
        public void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        public void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
    }
    internal delegate System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> PageHandlerExecutorDelegate(object handler, object[] arguments);
    internal partial class PageActionInvokerCacheEntry
    {
        public PageActionInvokerCacheEntry(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> viewDataFactory, System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> pageFactory, System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> releasePage, System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> modelFactory, System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> releaseModel, System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object, System.Threading.Tasks.Task> propertyBinder, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerExecutorDelegate[] handlerExecutors, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate[] handlerBinders, System.Collections.Generic.IReadOnlyList<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> viewStartFactories, Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cacheableFilters) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CacheableFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate[] HandlerBinders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerExecutorDelegate[] HandlerExecutors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> ModelFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> PageFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object, System.Threading.Tasks.Task> PropertyBinder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> ReleaseModel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> ReleasePage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> ViewDataFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> ViewStartFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class PageActionInvokerProvider : Microsoft.AspNetCore.Mvc.Abstractions.IActionInvokerProvider
    {
        public PageActionInvokerProvider(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader, Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider pageFactoryProvider, Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider modelFactoryProvider, Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider razorPageFactoryProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcViewOptions, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector selector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper) { }
        public PageActionInvokerProvider(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader, Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider pageFactoryProvider, Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider modelFactoryProvider, Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider razorPageFactoryProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcViewOptions, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector selector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor) { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal System.Collections.Generic.List<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> GetViewStartFactories(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        internal partial class InnerCache
        {
            public InnerCache(int version) { }
            public System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry> Entries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
    }
    internal static partial class PageBinderFactory
    {
        internal static readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate NullHandlerBinder = (context, arguments) => System.Threading.Tasks.Task.CompletedTask;
        internal static readonly System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object, System.Threading.Tasks.Task> NullPropertyBinder = (context, arguments) => System.Threading.Tasks.Task.CompletedTask;
        public static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate CreateHandlerBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handler, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { throw null; }
        public static System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object, System.Threading.Tasks.Task> CreatePropertyBinder(Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
    }
    internal delegate System.Threading.Tasks.Task PageHandlerBinderDelegate(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, System.Collections.Generic.IDictionary<string, object> arguments);
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class RazorPagesRazorViewEngineOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>
    {
        public RazorPagesRazorViewEngineOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptions) { }
        public void Configure(Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions options) { }
    }
    internal partial class RazorPagesOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>
    {
        public RazorPagesOptionsSetup(System.IServiceProvider serviceProvider) { }
        public void Configure(Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions options) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal static partial class PageLoggerExtensions
    {
        public const string PageFilter = "Page Filter";
        public static void AfterExecutingMethodOnFilter(this Microsoft.Extensions.Logging.ILogger logger, string filterType, string methodName, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void BeforeExecutingMethodOnFilter(this Microsoft.Extensions.Logging.ILogger logger, string filterType, string methodName, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void ExecutedHandlerMethod(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handler, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public static void ExecutedImplicitHandlerMethod(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public static void ExecutedPageFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { }
        public static void ExecutedPageModelFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { }
        public static void ExecutingHandlerMethod(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handler, object[] arguments) { }
        public static void ExecutingImplicitHandlerMethod(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { }
        public static void ExecutingPageFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { }
        public static void ExecutingPageModelFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.RazorPages.PageContext context) { }
        public static void NotMostEffectiveFilter(this Microsoft.Extensions.Logging.ILogger logger, System.Type policyType) { }
        public static void PageFilterShortCircuited(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
    }
}