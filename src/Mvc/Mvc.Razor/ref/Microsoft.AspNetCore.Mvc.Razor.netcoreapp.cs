// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public partial class CompiledRazorAssemblyApplicationPartFactory : Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartFactory
    {
        public CompiledRazorAssemblyApplicationPartFactory() { }
        public override System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetApplicationParts(System.Reflection.Assembly assembly) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> GetDefaultApplicationParts(System.Reflection.Assembly assembly) { throw null; }
    }
    public partial class CompiledRazorAssemblyPart : Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart, Microsoft.AspNetCore.Mvc.ApplicationParts.IRazorCompiledItemProvider
    {
        public CompiledRazorAssemblyPart(System.Reflection.Assembly assembly) { }
        public System.Reflection.Assembly Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem> Microsoft.AspNetCore.Mvc.ApplicationParts.IRazorCompiledItemProvider.CompiledItems { get { throw null; } }
        public override string Name { get { throw null; } }
    }
    public partial interface IRazorCompiledItemProvider
    {
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem> CompiledItems { get; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed partial class AfterViewPageEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.Razor.AfterViewPage";
        public AfterViewPageEventData(Microsoft.AspNetCore.Mvc.Razor.IRazorPage page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.IRazorPage Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class BeforeViewPageEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage";
        public BeforeViewPageEventData(Microsoft.AspNetCore.Mvc.Razor.IRazorPage page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.IRazorPage Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor
{
    public partial class HelperResult : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public HelperResult(System.Func<System.IO.TextWriter, System.Threading.Tasks.Task> asyncAction) { }
        public System.Func<System.IO.TextWriter, System.Threading.Tasks.Task> WriteAction { get { throw null; } }
        public virtual void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial interface IRazorPage
    {
        Microsoft.AspNetCore.Html.IHtmlContent BodyContent { get; set; }
        bool IsLayoutBeingRendered { get; set; }
        string Layout { get; set; }
        string Path { get; set; }
        System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> PreviousSectionWriters { get; set; }
        System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> SectionWriters { get; }
        Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
        void EnsureRenderedBodyOrSections();
        System.Threading.Tasks.Task ExecuteAsync();
    }
    public partial interface IRazorPageActivator
    {
        void Activate(Microsoft.AspNetCore.Mvc.Razor.IRazorPage page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context);
    }
    public partial interface IRazorPageFactoryProvider
    {
        Microsoft.AspNetCore.Mvc.Razor.RazorPageFactoryResult CreateFactory(string relativePath);
    }
    public partial interface IRazorViewEngine : Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine
    {
        Microsoft.AspNetCore.Mvc.Razor.RazorPageResult FindPage(Microsoft.AspNetCore.Mvc.ActionContext context, string pageName);
        string GetAbsolutePath(string executingFilePath, string pagePath);
        Microsoft.AspNetCore.Mvc.Razor.RazorPageResult GetPage(string executingFilePath, string pagePath);
    }
    public partial interface ITagHelperActivator
    {
        TTagHelper Create<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper;
    }
    public partial interface ITagHelperFactory
    {
        TTagHelper CreateTagHelper<TTagHelper>(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper;
    }
    public partial interface ITagHelperInitializer<TTagHelper> where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper
    {
        void Initialize(TTagHelper helper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context);
    }
    public partial interface IViewLocationExpander
    {
        System.Collections.Generic.IEnumerable<string> ExpandViewLocations(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context, System.Collections.Generic.IEnumerable<string> viewLocations);
        void PopulateValues(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context);
    }
    public partial class LanguageViewLocationExpander : Microsoft.AspNetCore.Mvc.Razor.IViewLocationExpander
    {
        public LanguageViewLocationExpander() { }
        public LanguageViewLocationExpander(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { }
        public virtual System.Collections.Generic.IEnumerable<string> ExpandViewLocations(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context, System.Collections.Generic.IEnumerable<string> viewLocations) { throw null; }
        public void PopulateValues(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context) { }
    }
    public enum LanguageViewLocationExpanderFormat
    {
        SubFolder = 0,
        Suffix = 1,
    }
    public abstract partial class RazorPage : Microsoft.AspNetCore.Mvc.Razor.RazorPageBase
    {
        protected RazorPage() { }
        public Microsoft.AspNetCore.Http.HttpContext Context { get { throw null; } }
        public override void BeginContext(int position, int length, bool isLiteral) { }
        public override void DefineSection(string name, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate section) { }
        public override void EndContext() { }
        public override void EnsureRenderedBodyOrSections() { }
        public void IgnoreBody() { }
        public void IgnoreSection(string sectionName) { }
        public bool IsSectionDefined(string name) { throw null; }
        protected virtual Microsoft.AspNetCore.Html.IHtmlContent RenderBody() { throw null; }
        public Microsoft.AspNetCore.Html.HtmlString RenderSection(string name) { throw null; }
        public Microsoft.AspNetCore.Html.HtmlString RenderSection(string name, bool required) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.HtmlString> RenderSectionAsync(string name) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.HtmlString> RenderSectionAsync(string name, bool required) { throw null; }
    }
    public partial class RazorPageActivator : Microsoft.AspNetCore.Mvc.Razor.IRazorPageActivator
    {
        public RazorPageActivator(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory, Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper jsonHelper, System.Diagnostics.DiagnosticSource diagnosticSource, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider modelExpressionProvider) { }
        public void Activate(Microsoft.AspNetCore.Mvc.Razor.IRazorPage page, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { }
    }
    public abstract partial class RazorPageBase : Microsoft.AspNetCore.Mvc.Razor.IRazorPage
    {
        protected RazorPageBase() { }
        public Microsoft.AspNetCore.Html.IHtmlContent BodyContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public System.Diagnostics.DiagnosticSource DiagnosticSource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsLayoutBeingRendered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Layout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.IO.TextWriter Output { get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> PreviousSectionWriters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> SectionWriters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } }
        public virtual System.Security.Claims.ClaimsPrincipal User { get { throw null; } }
        public dynamic ViewBag { get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void AddHtmlAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral) { }
        public void BeginAddHtmlAttributeValues(Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext executionContext, string attributeName, int attributeValuesCount, Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle attributeValueStyle) { }
        public abstract void BeginContext(int position, int length, bool isLiteral);
        public virtual void BeginWriteAttribute(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount) { }
        public void BeginWriteTagHelperAttribute() { }
        public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
        public virtual void DefineSection(string name, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate section) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected void DefineSection(string name, System.Func<object, System.Threading.Tasks.Task> section) { }
        public void EndAddHtmlAttributeValues(Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext executionContext) { }
        public abstract void EndContext();
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent EndTagHelperWritingScope() { throw null; }
        public virtual void EndWriteAttribute() { }
        public string EndWriteTagHelperAttribute() { throw null; }
        public abstract void EnsureRenderedBodyOrSections();
        public abstract System.Threading.Tasks.Task ExecuteAsync();
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.HtmlString> FlushAsync() { throw null; }
        public virtual string Href(string contentPath) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public string InvalidTagHelperIndexerAssignment(string attributeName, string tagHelperTypeName, string propertyName) { throw null; }
        protected internal virtual System.IO.TextWriter PopWriter() { throw null; }
        protected internal virtual void PushWriter(System.IO.TextWriter writer) { }
        public virtual Microsoft.AspNetCore.Html.HtmlString SetAntiforgeryCookieAndHeader() { throw null; }
        public void StartTagHelperWritingScope(System.Text.Encodings.Web.HtmlEncoder encoder) { }
        public virtual void Write(object value) { }
        public virtual void Write(string value) { }
        public void WriteAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral) { }
        public virtual void WriteLiteral(object value) { }
        public virtual void WriteLiteral(string value) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RazorPageFactoryResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public RazorPageFactoryResult(Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor viewDescriptor, System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage> razorPageFactory) { throw null; }
        public System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage> RazorPageFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Success { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor ViewDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RazorPageResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public RazorPageResult(string name, Microsoft.AspNetCore.Mvc.Razor.IRazorPage page) { throw null; }
        public RazorPageResult(string name, System.Collections.Generic.IEnumerable<string> searchedLocations) { throw null; }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.IRazorPage Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> SearchedLocations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class RazorPage<TModel> : Microsoft.AspNetCore.Mvc.Razor.RazorPage
    {
        protected RazorPage() { }
        public TModel Model { get { throw null; } }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RazorView : Microsoft.AspNetCore.Mvc.ViewEngines.IView
    {
        public RazorView(Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine viewEngine, Microsoft.AspNetCore.Mvc.Razor.IRazorPageActivator pageActivator, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Razor.IRazorPage> viewStartPages, Microsoft.AspNetCore.Mvc.Razor.IRazorPage razorPage, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.Diagnostics.DiagnosticListener diagnosticListener) { }
        public string Path { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.IRazorPage RazorPage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Razor.IRazorPage> ViewStartPages { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task RenderAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { throw null; }
    }
    public partial class RazorViewEngine : Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine, Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine
    {
        public static readonly string ViewExtension;
        public RazorViewEngine(Microsoft.AspNetCore.Mvc.Razor.IRazorPageFactoryProvider pageFactory, Microsoft.AspNetCore.Mvc.Razor.IRazorPageActivator pageActivator, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener) { }
        protected Microsoft.Extensions.Caching.Memory.IMemoryCache ViewLookupCache { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Razor.RazorPageResult FindPage(Microsoft.AspNetCore.Mvc.ActionContext context, string pageName) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult FindView(Microsoft.AspNetCore.Mvc.ActionContext context, string viewName, bool isMainPage) { throw null; }
        public string GetAbsolutePath(string executingFilePath, string pagePath) { throw null; }
        public static string GetNormalizedRouteValue(Microsoft.AspNetCore.Mvc.ActionContext context, string key) { throw null; }
        public Microsoft.AspNetCore.Mvc.Razor.RazorPageResult GetPage(string executingFilePath, string pagePath) { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage) { throw null; }
    }
    public partial class RazorViewEngineOptions
    {
        public RazorViewEngineOptions() { }
        public System.Collections.Generic.IList<string> AreaPageViewLocationFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<string> AreaViewLocationFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<string> PageViewLocationFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.IViewLocationExpander> ViewLocationExpanders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<string> ViewLocationFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public delegate System.Threading.Tasks.Task RenderAsyncDelegate();
    public partial class TagHelperInitializer<TTagHelper> : Microsoft.AspNetCore.Mvc.Razor.ITagHelperInitializer<TTagHelper> where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper
    {
        public TagHelperInitializer(System.Action<TTagHelper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext> action) { }
        public void Initialize(TTagHelper helper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { }
    }
    public partial class ViewLocationExpanderContext
    {
        public ViewLocationExpanderContext(Microsoft.AspNetCore.Mvc.ActionContext actionContext, string viewName, string controllerName, string areaName, string pageName, bool isMainPage) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string AreaName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ControllerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsMainPage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string PageName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<string, string> Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public partial class CompiledViewDescriptor
    {
        public CompiledViewDescriptor() { }
        public CompiledViewDescriptor(Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem item) { }
        public CompiledViewDescriptor(Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem item, Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute attribute) { }
        public System.Collections.Generic.IList<Microsoft.Extensions.Primitives.IChangeToken> ExpirationTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem Item { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RelativePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type Type { get { throw null; } }
        [System.ObsoleteAttribute("Use Item instead. RazorViewAttribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
        public Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute ViewAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IViewCompiler
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> CompileAsync(string relativePath);
    }
    public partial interface IViewCompilerProvider
    {
        Microsoft.AspNetCore.Mvc.Razor.Compilation.IViewCompiler GetCompiler();
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=true)]
    [System.ObsoleteAttribute("This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    public partial class RazorViewAttribute : System.Attribute
    {
        public RazorViewAttribute(string path, System.Type viewType) { }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type ViewType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ViewsFeature
    {
        public ViewsFeature() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Razor.Compilation.CompiledViewDescriptor> ViewDescriptors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    public sealed partial class TagHelperMemoryCacheProvider
    {
        public TagHelperMemoryCacheProvider() { }
        public Microsoft.Extensions.Caching.Memory.IMemoryCache Cache { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class RazorInjectAttribute : System.Attribute
    {
        public RazorInjectAttribute() { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("body")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class BodyTagHelper : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperComponentTagHelper
    {
        public BodyTagHelper(Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager manager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("head")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class HeadTagHelper : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperComponentTagHelper
    {
        public HeadTagHelper(Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager manager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
    }
    public partial interface ITagHelperComponentManager
    {
        System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent> Components { get; }
    }
    public partial interface ITagHelperComponentPropertyActivator
    {
        void Activate(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context, Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent tagHelperComponent);
    }
    public abstract partial class TagHelperComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
    {
        public TagHelperComponentTagHelper(Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentManager manager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute]
        public Microsoft.AspNetCore.Mvc.Razor.TagHelpers.ITagHelperComponentPropertyActivator PropertyActivator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void Init(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { throw null; }
    }
    public partial class TagHelperFeature
    {
        public TagHelperFeature() { }
        public System.Collections.Generic.IList<System.Reflection.TypeInfo> TagHelpers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class TagHelperFeatureProvider : Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider, Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider<Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperFeature>
    {
        public TagHelperFeatureProvider() { }
        protected virtual bool IncludePart(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart part) { throw null; }
        protected virtual bool IncludeType(System.Reflection.TypeInfo type) { throw null; }
        public void PopulateFeature(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperFeature feature) { }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("*", Attributes="[itemid^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("a", Attributes="[href^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("applet", Attributes="[archive^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("area", Attributes="[href^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("audio", Attributes="[src^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("base", Attributes="[href^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("blockquote", Attributes="[cite^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("button", Attributes="[formaction^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("del", Attributes="[cite^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("embed", Attributes="[src^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("form", Attributes="[action^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("html", Attributes="[manifest^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("iframe", Attributes="[src^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("img", Attributes="[src^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("img", Attributes="[srcset^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("input", Attributes="[formaction^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("input", Attributes="[src^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("ins", Attributes="[cite^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="[href^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("menuitem", Attributes="[icon^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("object", Attributes="[archive^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("object", Attributes="[data^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("q", Attributes="[cite^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="[src^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("source", Attributes="[src^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("source", Attributes="[srcset^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("track", Attributes="[src^='~/']", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("video", Attributes="[poster^='~/']")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("video", Attributes="[src^='~/']")]
    public partial class UrlResolutionTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
    {
        public UrlResolutionTagHelper(Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory urlHelperFactory, System.Text.Encodings.Web.HtmlEncoder htmlEncoder) { }
        protected System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override int Order { get { throw null; } }
        protected Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory UrlHelperFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void Process(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { }
        protected void ProcessUrlAttribute(string attributeName, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { }
        protected bool TryResolveUrl(string url, out Microsoft.AspNetCore.Html.IHtmlContent resolvedUrl) { throw null; }
        protected bool TryResolveUrl(string url, out string resolvedUrl) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcRazorMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddRazorOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddTagHelpersAsServices(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder InitializeTagHelper<TTagHelper>(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<TTagHelper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext> initialize) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
    public static partial class MvcRazorMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorViewEngine(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorViewEngine(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddTagHelpersAsServices(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder InitializeTagHelper<TTagHelper>(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<TTagHelper, Microsoft.AspNetCore.Mvc.Rendering.ViewContext> initialize) where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper { throw null; }
    }
}
