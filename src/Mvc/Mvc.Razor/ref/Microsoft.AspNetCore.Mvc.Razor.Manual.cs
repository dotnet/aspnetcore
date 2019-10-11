// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    internal partial class RazorCompiledItemFeatureProvider
    {
        public RazorCompiledItemFeatureProvider() { }
        public void PopulateFeature(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, Microsoft.AspNetCore.Mvc.Razor.Compilation.ViewsFeature feature) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor
{
    public partial class RazorPageActivator : Microsoft.AspNetCore.Mvc.Razor.IRazorPageActivator
    {
        internal Microsoft.AspNetCore.Mvc.Razor.RazorPagePropertyActivator GetOrAddCacheEntry(Microsoft.AspNetCore.Mvc.Razor.IRazorPage page) { throw null; }
    }
    internal partial class DefaultTagHelperFactory : Microsoft.AspNetCore.Mvc.Razor.ITagHelperFactory
    {
        private readonly Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator _activator;
        private static readonly System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>> _createActivateInfo;
        private readonly System.Func<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[]> _getPropertiesToActivate;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[]> _injectActions;
        public DefaultTagHelperFactory(Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator activator) { }
        private static Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext> CreateActivateInfo(System.Reflection.PropertyInfo property) { throw null; }
        public TTagHelper CreateTagHelper<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
        private static void InitializeTagHelper<TTagHelper>(TTagHelper tagHelper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { }
    }
    public partial class RazorView
    {
        internal System.Action<Microsoft.AspNetCore.Mvc.Razor.IRazorPage, Microsoft.AspNetCore.Mvc.Rendering.ViewContext> OnAfterPageActivated { get { throw null; } set { } }
    }
    internal partial class RazorPagePropertyActivator
    {
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider _metadataProvider;
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> _nestedFactory;
        private readonly Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[] _propertyActivators;
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> _rootFactory;
        private readonly System.Type _viewDataDictionaryType;
        public RazorPagePropertyActivator(System.Type pageType, System.Type declaredModelType, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Razor.RazorPagePropertyActivator.PropertyValueAccessors propertyValueAccessors) { }
        public void Activate(object page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { }
        private static Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext> CreateActivateInfo(System.Reflection.PropertyInfo property, Microsoft.AspNetCore.Mvc.Razor.RazorPagePropertyActivator.PropertyValueAccessors valueAccessors) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary CreateViewDataDictionary(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { throw null; }
        public partial class PropertyValueAccessors
        {
            private System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _DiagnosticSourceAccessor_k__BackingField;
            private System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _HtmlEncoderAccessor_k__BackingField;
            private System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _JsonHelperAccessor_k__BackingField;
            private System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _ModelExpressionProviderAccessor_k__BackingField;
            private System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> _UrlHelperAccessor_k__BackingField;
            public PropertyValueAccessors() { }
            public System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> DiagnosticSourceAccessor { get { throw null; } set { } }
            public System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> HtmlEncoderAccessor { get { throw null; } set { } }
            public System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> JsonHelperAccessor { get { throw null; } set { } }
            public System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> ModelExpressionProviderAccessor { get { throw null; } set { } }
            public System.Func<Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> UrlHelperAccessor { get { throw null; } set { } }
        }
    }
    internal partial interface IModelTypeProvider
    {
        System.Type GetModelType();
    }
    internal static partial class RazorFileHierarchy
    {
        private const string ViewStartFileName = "_ViewStart.cshtml";
        public static System.Collections.Generic.IEnumerable<string> GetViewStartPaths(string path) { throw null; }
    }
    internal partial class RazorViewEngineOptionsSetup
    {
        public RazorViewEngineOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions options) { }
    }
    internal partial class DefaultViewCompiler : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler
    {
        private readonly System.Collections.Generic.Dictionary<string, System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor>> _compiledViews;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _normalizedPathCache;
        public DefaultViewCompiler(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> compiledViews, Microsoft.Extensions.Logging.ILogger logger) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> CompileAsync(string relativePath) { throw null; }
        private string GetNormalizedPath(string relativePath) { throw null; }
    }
    internal static partial class ViewPath
    {
        public static string NormalizePath(string path) { throw null; }
    }
    internal partial class ServiceBasedTagHelperActivator : Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator
    {
        public ServiceBasedTagHelperActivator() { }
        public TTagHelper Create<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
    internal partial class TagHelperComponentManager : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager
    {
        private readonly System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> _Components_k__BackingField;
        public TagHelperComponentManager(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> tagHelperComponents) { }
        public System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> Components { get { throw null; } }
    }
    internal static partial class Resources
    {
        private static System.Resources.ResourceManager s_resourceManager;
        private static System.Globalization.CultureInfo _Culture_k__BackingField;
        internal static string ArgumentCannotBeNullOrEmpty { get { throw null; } }
        internal static string CompilationFailed { get { throw null; } }
        internal static string Compilation_MissingReferences { get { throw null; } }
        internal static string CompiledViewDescriptor_NoData { get { throw null; } }
        internal static string CouldNotResolveApplicationRelativeUrl_TagHelper { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { get { throw null; } set { } }
        internal static string FileProvidersAreRequired { get { throw null; } }
        internal static string FlushPointCannotBeInvoked { get { throw null; } }
        internal static string GeneratedCodeFileName { get { throw null; } }
        internal static string LayoutCannotBeLocated { get { throw null; } }
        internal static string LayoutCannotBeRendered { get { throw null; } }
        internal static string LayoutHasCircularReference { get { throw null; } }
        internal static string PropertyMustBeSet { get { throw null; } }
        internal static string RazorPage_CannotFlushWhileInAWritingScope { get { throw null; } }
        internal static string RazorPage_InvalidTagHelperIndexerAssignment { get { throw null; } }
        internal static string RazorPage_MethodCannotBeCalled { get { throw null; } }
        internal static string RazorPage_NestingAttributeWritingScopesNotSupported { get { throw null; } }
        internal static string RazorPage_ThereIsNoActiveWritingScopeToEnd { get { throw null; } }
        internal static string RazorProject_PathMustStartWithForwardSlash { get { throw null; } }
        internal static string RazorViewCompiler_ViewPathsDifferOnlyInCase { get { throw null; } }
        internal static string RenderBodyNotCalled { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string SectionAlreadyDefined { get { throw null; } }
        internal static string SectionAlreadyRendered { get { throw null; } }
        internal static string SectionNotDefined { get { throw null; } }
        internal static string SectionsNotRendered { get { throw null; } }
        internal static string UnsupportedDebugInformationFormat { get { throw null; } }
        internal static string ViewContextMustBeSet { get { throw null; } }
        internal static string ViewLocationFormatsIsRequired { get { throw null; } }
        internal static string FormatCompilation_MissingReferences(object p0) { throw null; }
        internal static string FormatCompiledViewDescriptor_NoData(object p0, object p1) { throw null; }
        internal static string FormatCouldNotResolveApplicationRelativeUrl_TagHelper(object p0, object p1, object p2, object p3, object p4, object p5) { throw null; }
        internal static string FormatFileProvidersAreRequired(object p0, object p1, object p2) { throw null; }
        internal static string FormatFlushPointCannotBeInvoked(object p0) { throw null; }
        internal static string FormatLayoutCannotBeLocated(object p0, object p1) { throw null; }
        internal static string FormatLayoutCannotBeRendered(object p0, object p1) { throw null; }
        internal static string FormatLayoutHasCircularReference(object p0, object p1) { throw null; }
        internal static string FormatPropertyMustBeSet(object p0, object p1) { throw null; }
        internal static string FormatRazorPage_CannotFlushWhileInAWritingScope(object p0, object p1) { throw null; }
        internal static string FormatRazorPage_InvalidTagHelperIndexerAssignment(object p0, object p1, object p2) { throw null; }
        internal static string FormatRazorPage_MethodCannotBeCalled(object p0, object p1) { throw null; }
        internal static string FormatRenderBodyNotCalled(object p0, object p1, object p2) { throw null; }
        internal static string FormatSectionAlreadyDefined(object p0) { throw null; }
        internal static string FormatSectionAlreadyRendered(object p0, object p1, object p2) { throw null; }
        internal static string FormatSectionNotDefined(object p0, object p1, object p2) { throw null; }
        internal static string FormatSectionsNotRendered(object p0, object p1, object p2) { throw null; }
        internal static string FormatUnsupportedDebugInformationFormat(object p0) { throw null; }
        internal static string FormatViewContextMustBeSet(object p0, object p1) { throw null; }
        internal static string FormatViewLocationFormatsIsRequired(object p0) { throw null; }
        internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
        private static string GetResourceString(string resourceKey, string[] formatterNames) { throw null; }
    }
    internal partial class DefaultRazorPageFactoryProvider : Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider _viewCompilerProvider;
        public DefaultRazorPageFactoryProvider(Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider viewCompilerProvider) { }
        private Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler Compiler { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.RazorPageFactoryResult CreateFactory(string relativePath) { throw null; }
    }
    public partial class RazorViewEngine : Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine
    {
        internal System.Collections.Generic.IEnumerable<string> GetViewLocationFormats(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    internal partial class DefaultFileVersionProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider
    {
        private static readonly char[] QueryStringAndFragmentTokens;
        private const string VersionKey = "v";
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _Cache_k__BackingField;
        private readonly Microsoft.Extensions.FileProviders.IFileProvider _FileProvider_k__BackingField;
        public DefaultFileVersionProvider(Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, Microsoft.AspNetCore.Mvc.Razor.Infrastructure.TagHelperMemoryCacheProvider cacheProvider) { }
        public Microsoft.Extensions.Caching.Memory.IMemoryCache Cache { get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { get { throw null; } }
        public string AddFileVersionToPath(Microsoft.AspNetCore.Http.PathString requestPathBase, string path) { throw null; }
        private static string GetHashForFile(Microsoft.Extensions.FileProviders.IFileInfo fileInfo) { throw null; }
    }
    internal partial class DefaultTagHelperActivator : Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator
    {
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache _typeActivatorCache;
        public DefaultTagHelperActivator(Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache typeActivatorCache) { }
        public TTagHelper Create<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
    public sealed partial class TagHelperMemoryCacheProvider
    {
        private Microsoft.Extensions.Caching.Memory.IMemoryCache _Cache_k__BackingField;
        public TagHelperMemoryCacheProvider() { }
        public Microsoft.Extensions.Caching.Memory.IMemoryCache Cache { get { throw null; } internal set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    internal partial class TagHelperComponentPropertyActivator : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentPropertyActivator
    {
        private static readonly System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>> _createActivateInfo;
        private readonly System.Func<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[]> _getPropertiesToActivate;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[]> _propertiesToActivate;
        public TagHelperComponentPropertyActivator() { }
        public void Activate(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context, Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent tagHelperComponent) { }
        private static Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext> CreateActivateInfo(System.Reflection.PropertyInfo property) { throw null; }
        private static Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.Rendering.ViewContext>[] GetPropertiesToActivate(System.Type type) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal partial class DefaultViewCompilerProvider : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.Razor.Compilation.DefaultViewCompiler _compiler;
        public DefaultViewCompilerProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationPartManager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler GetCompiler() { throw null; }
    }
    internal partial class DefaultViewCompiler : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler
    {
        private readonly System.Collections.Generic.Dictionary<string, System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor>> _compiledViews;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _normalizedPathCache;
        public DefaultViewCompiler(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> compiledViews, Microsoft.Extensions.Logging.ILogger logger) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> CompileAsync(string relativePath) { throw null; }
        private string GetNormalizedPath(string relativePath) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class MvcRazorMvcViewOptionsSetup
    {
        private readonly Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine _razorViewEngine;
        public MvcRazorMvcViewOptionsSetup(Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine razorViewEngine) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcViewOptions options) { }
    }
}