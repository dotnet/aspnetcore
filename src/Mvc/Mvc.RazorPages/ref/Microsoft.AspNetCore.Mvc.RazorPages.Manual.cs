// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
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
}
namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
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
    internal partial class CompiledPageRouteModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelProvider
    {
        public CompiledPageRouteModelProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptionsAccessor, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.ApplicationModels.CompiledPageRouteModelProvider> logger) { }
        public int Order { get { throw null; } }
        protected virtual Microsoft.AspNetCore.Mvc.Razor.Compilation.ViewsFeature GetViewFeature(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
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
    internal sealed partial class HandleOptionsRequestsPageFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IPageFilter
    {
        public HandleOptionsRequestsPageFilter() { }
        public int Order { get { throw null; } }
        public void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        public void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
    }
    internal delegate System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> PageHandlerExecutorDelegate(object handler, object[] arguments);
    internal partial class PageActionInvoker : Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker, Microsoft.AspNetCore.Mvc.Abstractions.IActionInvoker
    {
        public PageActionInvoker(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector handlerMethodSelector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filterMetadata, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions htmlHelperOptions) : base (default(System.Diagnostics.DiagnosticListener), default(Microsoft.Extensions.Logging.ILogger), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper), default(Microsoft.AspNetCore.Mvc.ActionContext), default(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[]), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>)) { }
        protected override System.Threading.Tasks.Task InvokeInnerFilterAsync() { throw null; }
        protected override System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw null; }
        protected override void ReleaseResources() { }
    }
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
}