// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.IntegrationTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.RazorPages.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

namespace Microsoft.AspNetCore.Builder
{
    internal partial class CompiledPageRouteModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelProvider
    {
        private static readonly string RazorPageDocumentKind;
        private static readonly string RouteTemplateKey;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager _applicationManager;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions _pagesOptions;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelFactory _routeModelFactory;
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
        private static readonly string IndexFileName;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly string _normalizedAreaRootDirectory;
        private readonly string _normalizedRootDirectory;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions _options;
        private static readonly System.Action<Microsoft.Extensions.Logging.ILogger, string, System.Exception> _unsupportedAreaPath;
        public PageRouteModelFactory(Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions options, Microsoft.Extensions.Logging.ILogger logger) { }
        private static string CreateAreaRoute(string areaName, string viewEnginePath) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel CreateAreaRouteModel(string relativePath, string routeTemplate) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel CreateRouteModel(string relativePath, string routeTemplate) { throw null; }
        private static Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel CreateSelectorModel(string prefix, string routeTemplate) { throw null; }
        private string GetViewEnginePath(string rootDirectory, string path) { throw null; }
        private static string NormalizeDirectory(string directory) { throw null; }
        private static void PopulateRouteModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel model, string pageRoute, string routeTemplate) { }
        internal bool TryParseAreaPath(string relativePath, out (string areaName, string viewEnginePath) result) { throw null; }
    }
    internal partial class AuthorizationPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        private readonly Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider _policyProvider;
        public AuthorizationPageApplicationModelProvider(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    internal partial class DefaultPageApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelProvider
    {
        private const string ModelPropertyName = "Model";
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandleOptionsRequestsPageFilter _handleOptionsRequestsFilter;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider _modelMetadataProvider;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelPartsProvider _pageApplicationModelPartsProvider;
        private readonly Microsoft.AspNetCore.Mvc.Filters.PageHandlerPageFilter _pageHandlerPageFilter;
        private readonly Microsoft.AspNetCore.Mvc.Filters.PageHandlerResultFilter _pageHandlerResultFilter;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions _razorPagesOptions;
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
        private readonly Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer _tempDataSerializer;
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
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        public ResponseCacheFilterApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context) { }
    }
    internal partial class CompiledPageRouteModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelProvider
    {
        private static readonly string RazorPageDocumentKind;
        private static readonly string RouteTemplateKey;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager _applicationManager;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions _pagesOptions;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelFactory _routeModelFactory;
        public CompiledPageRouteModelProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptionsAccessor, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.ApplicationModels.CompiledPageRouteModelProvider> logger) { }
        public int Order { get { throw null; } }
        private void CreateModels(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context) { }
        private System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> GetViewDescriptors(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationManager) { throw null; }
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
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor _actionDescriptor;
        private System.Collections.Generic.Dictionary<string, object> _arguments;
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor _handler;
        private Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext _handlerExecutedContext;
        private Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext _handlerExecutingContext;
        private Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext _handlerSelectedContext;
        private readonly Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions _htmlHelperOptions;
        private Microsoft.AspNetCore.Mvc.RazorPages.PageBase _page;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.PageContext _pageContext;
        private object _pageModel;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder _parameterBinder;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector _selector;
        private readonly Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory _tempDataFactory;
        private Microsoft.AspNetCore.Mvc.Rendering.ViewContext _viewContext;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry _CacheEntry_k__BackingField;
        public PageActionInvoker(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector handlerMethodSelector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filterMetadata, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions htmlHelperOptions) : base (default(System.Diagnostics.DiagnosticListener), default(Microsoft.Extensions.Logging.ILogger), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper), default(Microsoft.AspNetCore.Mvc.ActionContext), default(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[]), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>)) { }
        internal Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry CacheEntry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        private bool HasPageModel { get { throw null; } }
        internal Microsoft.AspNetCore.Mvc.RazorPages.PageContext PageContext { get { throw null; } }
        private System.Threading.Tasks.Task BindArgumentsAsync() { throw null; }
        protected override System.Threading.Tasks.Task InvokeInnerFilterAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        private System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext> InvokeNextPageFilterAwaitedAsync() { throw null; }
        protected override System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw null; }
        private System.Threading.Tasks.Task Next(ref Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvoker.State next, ref Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvoker.Scope scope, ref object state, ref bool isCompleted) { throw null; }
        private static object[] PrepareArguments(System.Collections.Generic.IDictionary<string, object> argumentsInDictionary, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handler) { throw null; }
        protected override void ReleaseResources() { }
        private static void Rethrow(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor SelectHandler() { throw null; }
        private enum Scope
        {
            Invoker = 0,
            Page = 1,
        }
        private enum State
        {
            PageBegin = 0,
            PageSelectHandlerBegin = 1,
            PageSelectHandlerNext = 2,
            PageSelectHandlerAsyncBegin = 3,
            PageSelectHandlerAsyncEnd = 4,
            PageSelectHandlerSync = 5,
            PageSelectHandlerEnd = 6,
            PageNext = 7,
            PageAsyncBegin = 8,
            PageAsyncEnd = 9,
            PageSyncBegin = 10,
            PageSyncEnd = 11,
            PageInside = 12,
            PageEnd = 13,
        }
    }
    internal partial class PageActionInvokerCacheEntry
    {
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor _ActionDescriptor_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly Microsoft.AspNetCore.Mvc.Filters.FilterItem[] _CacheableFilters_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate[] _HandlerBinders_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerExecutorDelegate[] _HandlerExecutors_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> _ModelFactory_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _PageFactory_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object, System.Threading.Tasks.Task> _PropertyBinder_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> _ReleaseModel_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _ReleasePage_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> _ViewDataFactory_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.Collections.Generic.IReadOnlyList<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> _ViewStartFactories_k__BackingField;
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
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider _collectionProvider;
        private volatile Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerProvider.InnerCache _currentCache;
        private readonly System.Diagnostics.DiagnosticListener _diagnosticListener;
        private readonly Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] _filterProviders;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader _loader;
        private readonly Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvoker> _logger;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper _mapper;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory _modelBinderFactory;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider _modelFactoryProvider;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider _modelMetadataProvider;
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        private readonly Microsoft.AspNetCore.Mvc.MvcViewOptions _mvcViewOptions;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider _pageFactoryProvider;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder _parameterBinder;
        private readonly Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider _razorPageFactoryProvider;
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector _selector;
        private readonly Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory _tempDataFactory;
        private readonly System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> _valueProviderFactories;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly int _Order_k__BackingField;
        public PageActionInvokerProvider(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader, Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider pageFactoryProvider, Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider modelFactoryProvider, Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider razorPageFactoryProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcViewOptions, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector selector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper) { }
        public PageActionInvokerProvider(Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageLoader loader, Microsoft.AspNetCore.Mvc.RazorPages.IPageFactoryProvider pageFactoryProvider, Microsoft.AspNetCore.Mvc.RazorPages.IPageModelFactoryProvider modelFactoryProvider, Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider razorPageFactoryProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions> mvcViewOptions, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageHandlerMethodSelector selector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor) { }
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerProvider.InnerCache CurrentCache { get { throw null; } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvoker CreateActionInvoker(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry CreateCacheEntry(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context, Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilters) { throw null; }
        private Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerBinderDelegate[] GetHandlerBinders(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
        private static Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageHandlerExecutorDelegate[] GetHandlerExecutors(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor) { throw null; }
        internal System.Collections.Generic.List<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> GetViewStartFactories(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        internal partial class InnerCache
        {
            [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
            [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
            private readonly System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvokerCacheEntry> _Entries_k__BackingField;
            [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
            [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
            private readonly int _Version_k__BackingField;
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
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private readonly partial struct BinderItem
        {
            private readonly object _dummy;
            public BinderItem(Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder modelBinder, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { throw null; }
            public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder ModelBinder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata ModelMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
    }
    internal delegate System.Threading.Tasks.Task PageHandlerBinderDelegate(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, System.Collections.Generic.IDictionary<string, object> arguments);
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class RazorPagesRazorViewEngineOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>
    {
        private readonly Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions _pagesOptions;
        public RazorPagesRazorViewEngineOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptions) { }
        private static string CombinePath(string path1, string path2) { throw null; }
        public void Configure(Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions options) { }
    }
}