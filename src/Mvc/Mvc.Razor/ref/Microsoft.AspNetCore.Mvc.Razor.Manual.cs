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
        public DefaultTagHelperFactory(Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator activator) { }
        public TTagHelper CreateTagHelper<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
    public partial class RazorView
    {
        internal System.Action<Microsoft.AspNetCore.Mvc.Razor.IRazorPage, Microsoft.AspNetCore.Mvc.Rendering.ViewContext> OnAfterPageActivated { get { throw null; } set { } }
    }
    internal partial class RazorPagePropertyActivator
    {
        public RazorPagePropertyActivator(System.Type pageType, System.Type declaredModelType, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Razor.RazorPagePropertyActivator.PropertyValueAccessors propertyValueAccessors) { }
        public void Activate(object page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { }
        internal Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary CreateViewDataDictionary(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { throw null; }
        public partial class PropertyValueAccessors
        {
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
        public static System.Collections.Generic.IEnumerable<string> GetViewStartPaths(string path) { throw null; }
    }
    internal partial class RazorViewEngineOptionsSetup
    {
        public RazorViewEngineOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions options) { }
    }
    internal partial class DefaultViewCompiler : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler
    {
        public DefaultViewCompiler(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> compiledViews, Microsoft.Extensions.Logging.ILogger logger) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> CompileAsync(string relativePath) { throw null; }
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
        public TagHelperComponentManager(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> tagHelperComponents) { }
        public System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> Components { get { throw null; } }
    }
    internal static partial class Resources
    {
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
    }
    public partial class RazorViewEngine : Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine
    {
        internal System.Collections.Generic.IEnumerable<string> GetViewLocationFormats(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal partial class DefaultRazorPageFactoryProvider : Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider
    {
        public DefaultRazorPageFactoryProvider(Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider viewCompilerProvider) { }
        public Microsoft.AspNetCore.Mvc.Razor.RazorPageFactoryResult CreateFactory(string relativePath) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    internal static partial class CryptographyAlgorithms
    {
        public static System.Security.Cryptography.SHA256 CreateSHA256() { throw null; }
    }

    internal partial class DefaultFileVersionProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider
    {
        public DefaultFileVersionProvider(Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, Microsoft.AspNetCore.Mvc.Razor.Infrastructure.TagHelperMemoryCacheProvider cacheProvider) { }
        public Microsoft.Extensions.Caching.Memory.IMemoryCache Cache { get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { get { throw null; } }
        public string AddFileVersionToPath(Microsoft.AspNetCore.Http.PathString requestPathBase, string path) { throw null; }
    }
    internal partial class DefaultTagHelperActivator : Microsoft.AspNetCore.Mvc.Razor.ITagHelperActivator
    {
        public DefaultTagHelperActivator(Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache typeActivatorCache) { }
        public TTagHelper Create<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
}

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    internal partial class TagHelperComponentPropertyActivator : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentPropertyActivator
    {
        public TagHelperComponentPropertyActivator() { }
        public void Activate(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context, Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent tagHelperComponent) { }
    }
}

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal partial class DefaultViewCompilerProvider : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompilerProvider
    {
        public DefaultViewCompilerProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager applicationPartManager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler GetCompiler() { throw null; }
    }
    internal partial class DefaultViewCompiler : Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler
    {
        public DefaultViewCompiler(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> compiledViews, Microsoft.Extensions.Logging.ILogger logger) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> CompileAsync(string relativePath) { throw null; }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class MvcRazorMvcViewOptionsSetup
    {
        public MvcRazorMvcViewOptionsSetup(Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine razorViewEngine) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcViewOptions options) { }
    }
}
