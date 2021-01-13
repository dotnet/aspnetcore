// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ComponentParameter
    {
        private object _dummy;
        public string Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public static (System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> parameterDefinitions, System.Collections.Generic.IList<object> parameterValues) FromParameterView(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ServerComponent
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ServerComponent(int sequence, string assemblyName, string typeName, System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> parametersDefinitions, System.Collections.Generic.IList<object> parameterValues, System.Guid invocationId) { throw null; }
        public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Guid InvocationId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> ParameterDefinitions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<object> ParameterValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Sequence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ServerComponentMarker
    {
        public const string ServerMarkerType = "server";
        private object _dummy;
        private int _dummyPrimitive;
        public string Descriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PrerenderId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int? Sequence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.ServerComponentMarker GetEndRecord() { throw null; }
        public static Microsoft.AspNetCore.Components.ServerComponentMarker NonPrerendered(int sequence, string descriptor) { throw null; }
        public static Microsoft.AspNetCore.Components.ServerComponentMarker Prerendered(int sequence, string descriptor) { throw null; }
    }
    internal static partial class ServerComponentSerializationSettings
    {
        public static readonly System.TimeSpan DataExpiration;
        public const string DataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1";
        public static readonly System.Text.Json.JsonSerializerOptions JsonSerializationOptions;
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    internal partial class DefaultViewComponentActivator : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentActivator
    {
        public DefaultViewComponentActivator(Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache typeActivatorCache) { }
        public object Create(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
        public void Release(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context, object viewComponent) { }
    }
    public partial class DefaultViewComponentHelper : Microsoft.AspNetCore.Mvc.IViewComponentHelper, Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware
    {
        internal System.Collections.Generic.IDictionary<string, object> GetArgumentDictionary(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentDescriptor descriptor, object arguments) { throw null; }
    }
    internal partial class DefaultViewComponentInvokerFactory : Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentInvokerFactory
    {
        public DefaultViewComponentInvokerFactory(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentFactory viewComponentFactory, Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentInvokerCache viewComponentInvokerCache, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentInvoker CreateInstance(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext context) { throw null; }
    }
    internal partial class ViewComponentInvokerCache
    {
        public ViewComponentInvokerCache(Microsoft.AspNetCore.Mvc.ViewComponents.IViewComponentDescriptorCollectionProvider collectionProvider) { }
        internal Microsoft.Extensions.Internal.ObjectMethodExecutor GetViewComponentMethodExecutor(Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext viewComponentContext) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    internal partial class PagedBufferedTextWriter : System.IO.TextWriter
    {
        public PagedBufferedTextWriter(System.Buffers.ArrayPool<char> pool, System.IO.TextWriter inner) { }
        public override System.Text.Encoding Encoding { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public override void Write(char value) { }
        public override void Write(char[] buffer) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(string value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
    }
    internal partial class CharArrayBufferSource : Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.CharArrayBufferSource Instance;
        public CharArrayBufferSource() { }
        public char[] Rent(int bufferSize) { throw null; }
        public void Return(char[] buffer) { }
    }
    internal partial interface ICharBufferSource
    {
        char[] Rent(int bufferSize);
        void Return(char[] buffer);
    }
    internal partial class PagedCharBuffer
    {
        public const int PageSize = 1024;
        public PagedCharBuffer(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource bufferSource) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource BufferSource { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Collections.Generic.List<char[]> Pages { get { throw null; } }
        public void Append(char value) { }
        public void Append(char[] buffer, int index, int count) { }
        public void Append(string value) { }
        public void Clear() { }
        public void Dispose() { }
    }
    internal partial class ViewBuffer : Microsoft.AspNetCore.Html.IHtmlContentBuilder
    {
        public static readonly int PartialViewPageSize;
        public static readonly int TagHelperPageSize;
        public static readonly int ViewComponentPageSize;
        public static readonly int ViewPageSize;
        public ViewBuffer(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, string name, int pageSize) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferPage this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Append(string unencoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(string encoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Clear() { throw null; }
        public void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
        public System.Threading.Tasks.Task WriteToAsync(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { throw null; }
    }
    internal partial class ViewBufferTextWriter : System.IO.TextWriter
    {
        public ViewBufferTextWriter(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer buffer, System.Text.Encoding encoding) { }
        public ViewBufferTextWriter(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer buffer, System.Text.Encoding encoding, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.IO.TextWriter inner) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer Buffer { get { throw null; } }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public bool Flushed { get { throw null; } }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public void Write(Microsoft.AspNetCore.Html.IHtmlContent value) { }
        public void Write(Microsoft.AspNetCore.Html.IHtmlContentContainer value) { }
        public override void Write(char value) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(object value) { }
        public override void Write(string value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public override void WriteLine() { }
        public override void WriteLine(object value) { }
        public override void WriteLine(string value) { }
        public override System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] value, int start, int offset) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    internal partial class ViewBufferPage
    {
        public ViewBufferPage(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] buffer) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] Buffer { get { throw null; } }
        public int Capacity { get { throw null; } }
        public int Count { get { throw null; } set { } }
        public bool IsFull { get { throw null; } }
        public void Append(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue value) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal partial class AutoValidateAntiforgeryTokenAuthorizationFilter : Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.ValidateAntiforgeryTokenAuthorizationFilter
    {
        public AutoValidateAntiforgeryTokenAuthorizationFilter(Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Antiforgery.IAntiforgery), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        protected override bool ShouldValidate(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
    }
    internal partial class ControllerSaveTempDataPropertyFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public ControllerSaveTempDataPropertyFilterFactory(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public bool IsReusable { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> TempDataProperties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class ControllerViewDataAttributeFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.IViewDataValuesProviderFeature
    {
        public ControllerViewDataAttributeFilter(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Subject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
        public void ProvideViewDataValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData) { }
    }
    internal partial class ControllerViewDataAttributeFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public ControllerViewDataAttributeFilterFactory(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> properties) { }
        public bool IsReusable { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class SaveTempDataFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        internal static readonly object SaveTempDataFilterContextKey;
        public SaveTempDataFilter(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory factory) { }
        public void OnResourceExecuted(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext context) { }
        public void OnResourceExecuting(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext context) { }
        public void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
        internal partial class SaveTempDataContext
        {
            public SaveTempDataContext() { }
            public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool RequestHasUnhandledException { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory TempDataDictionaryFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool TempDataSaved { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    internal partial class ControllerSaveTempDataPropertyFilter : Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.SaveTempDataPropertyFilterBase, Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public ControllerSaveTempDataPropertyFilter(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory factory) : base (default(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)) { }
        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
    }
    internal partial class TempDataApplicationModelProvider
    {
        public TempDataApplicationModelProvider(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class ValidateAntiforgeryTokenAuthorizationFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.ViewFeatures.IAntiforgeryPolicy
    {
        public ValidateAntiforgeryTokenAuthorizationFilter(Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnAuthorizationAsync(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
        protected virtual bool ShouldValidate(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
    }
    internal partial class ViewDataAttributeApplicationModelProvider
    {
        public ViewDataAttributeApplicationModelProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal static partial class ViewDataAttributePropertyProvider
    {
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> GetViewDataProperties(System.Type type) { throw null; }
    }
    internal partial interface ISaveTempDataCallback
    {
        void OnTempDataSaving(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData);
    }
    internal partial interface IViewDataValuesProviderFeature
    {
        void ProvideViewDataValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData);
    }
    internal abstract partial class SaveTempDataPropertyFilterBase : Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.ISaveTempDataCallback
    {
        protected readonly Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory _tempDataFactory;
        public SaveTempDataPropertyFilterBase(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory) { }
        public System.Collections.Generic.IDictionary<System.Reflection.PropertyInfo, object> OriginalValues { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { get { throw null; } set { } }
        public object Subject { get { throw null; } set { } }
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> GetTempDataProperties(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer, System.Type type) { throw null; }
        public void OnTempDataSaving(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData) { }
        protected void SetPropertyValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct LifecycleProperty
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LifecycleProperty(System.Reflection.PropertyInfo propertyInfo, string key) { throw null; }
        public string Key { get { throw null; } }
        public System.Reflection.PropertyInfo PropertyInfo { get { throw null; } }
        public object GetValue(object instance) { throw null; }
        public void SetValue(object instance, object value) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal static partial class CachedExpressionCompiler
    {
        public static System.Func<TModel, object> Process<TModel, TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression) { throw null; }
    }
    internal partial class ComponentRenderer : Microsoft.AspNetCore.Mvc.ViewFeatures.IComponentRenderer
    {
        public ComponentRenderer(Microsoft.AspNetCore.Mvc.ViewFeatures.StaticComponentRenderer staticComponentRenderer, Microsoft.AspNetCore.Mvc.ViewFeatures.ServerComponentSerializer serverComponentSerializer) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> RenderComponentAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, System.Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object parameters) { throw null; }
    }
    internal static partial class DefaultDisplayTemplates
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent BooleanTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent CollectionTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DecimalTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EmailAddressTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent HiddenInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent HtmlTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ObjectTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent StringTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        internal static System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> TriStateValues(bool? value) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent UrlTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
    }
    internal static partial class DefaultEditorTemplates
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent BooleanTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent CollectionTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DateInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DateTimeLocalInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DateTimeOffsetTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent DecimalTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent EmailAddressInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent FileCollectionInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent FileInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent HiddenInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent MonthInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent MultilineTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent NumberInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent ObjectTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent PasswordTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent PhoneNumberInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent StringTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent TimeInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent UrlInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContent WeekInputTemplate(Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper) { throw null; }
    }
    internal static partial class ExpressionHelper
    {
        public static string GetExpressionText(System.Linq.Expressions.LambdaExpression expression, System.Collections.Concurrent.ConcurrentDictionary<System.Linq.Expressions.LambdaExpression, string> expressionTextCache) { throw null; }
        public static string GetUncachedExpressionText(System.Linq.Expressions.LambdaExpression expression) { throw null; }
        public static bool IsSingleArgumentIndexer(System.Linq.Expressions.Expression expression) { throw null; }
    }
    internal static partial class ExpressionMetadataProvider
    {
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer FromLambdaExpression<TModel, TResult>(System.Linq.Expressions.Expression<System.Func<TModel, TResult>> expression, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TModel> viewData, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer FromStringExpression(string expression, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider) { throw null; }
    }
    internal partial class HtmlAttributePropertyHelper : Microsoft.Extensions.Internal.PropertyHelper
    {
        public HtmlAttributePropertyHelper(System.Reflection.PropertyInfo property) : base (default(System.Reflection.PropertyInfo)) { }
        public override string Name { get { throw null; } protected set { } }
        public static new Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type) { throw null; }
    }
    internal partial class HttpNavigationManager : Microsoft.AspNetCore.Components.NavigationManager, Microsoft.AspNetCore.Components.Routing.IHostEnvironmentNavigationManager
    {
        public HttpNavigationManager() { }
        void Microsoft.AspNetCore.Components.Routing.IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) { }
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
    internal partial interface IComponentRenderer
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Html.IHtmlContent> RenderComponentAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, System.Type componentType, Microsoft.AspNetCore.Mvc.Rendering.RenderMode renderMode, object parameters);
    }
    internal partial class LambdaExpressionComparer : System.Collections.Generic.IEqualityComparer<System.Linq.Expressions.LambdaExpression>
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.LambdaExpressionComparer Instance;
        public LambdaExpressionComparer() { }
        public bool Equals(System.Linq.Expressions.LambdaExpression lambdaExpression1, System.Linq.Expressions.LambdaExpression lambdaExpression2) { throw null; }
        public int GetHashCode(System.Linq.Expressions.LambdaExpression lambdaExpression) { throw null; }
    }
    internal partial class MemberExpressionCacheKeyComparer : System.Collections.Generic.IEqualityComparer<Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey>
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKeyComparer Instance;
        public MemberExpressionCacheKeyComparer() { }
        public bool Equals(Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey x, Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey y) { throw null; }
        public int GetHashCode(Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey obj) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct MemberExpressionCacheKey
    {
        private readonly object _dummy;
        public MemberExpressionCacheKey(System.Type modelType, System.Linq.Expressions.MemberExpression memberExpression) { throw null; }
        public MemberExpressionCacheKey(System.Type modelType, System.Reflection.MemberInfo[] members) { throw null; }
        public System.Linq.Expressions.MemberExpression MemberExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.MemberInfo[] Members { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type ModelType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey.Enumerator GetEnumerator() { throw null; }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey MakeCacheable() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator
        {
            private readonly System.Reflection.MemberInfo[] _members;
            private int _index;
            public Enumerator(in Microsoft.AspNetCore.Mvc.ViewFeatures.MemberExpressionCacheKey key) { throw null; }
            public System.Reflection.MemberInfo Current { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    internal static partial class Resources
    {
        internal static string ArgumentCannotBeNullOrEmpty { get { throw null; } }
        internal static string ArgumentPropertyUnexpectedType { get { throw null; } }
        internal static string Common_PartialViewNotFound { get { throw null; } }
        internal static string Common_PropertyNotFound { get { throw null; } }
        internal static string Common_TriState_False { get { throw null; } }
        internal static string Common_TriState_NotSet { get { throw null; } }
        internal static string Common_TriState_True { get { throw null; } }
        internal static string CreateModelExpression_NullModelMetadata { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string DeserializingTempData { get { throw null; } }
        internal static string Dictionary_DuplicateKey { get { throw null; } }
        internal static string DynamicViewData_ViewDataNull { get { throw null; } }
        internal static string ExpressionHelper_InvalidIndexerExpression { get { throw null; } }
        internal static string HtmlGenerator_FieldNameCannotBeNullOrEmpty { get { throw null; } }
        internal static string HtmlHelper_MissingSelectData { get { throw null; } }
        internal static string HtmlHelper_NotContextualized { get { throw null; } }
        internal static string HtmlHelper_NullModelMetadata { get { throw null; } }
        internal static string HtmlHelper_SelectExpressionNotEnumerable { get { throw null; } }
        internal static string HtmlHelper_TextAreaParameterOutOfRange { get { throw null; } }
        internal static string HtmlHelper_TypeNotSupported_ForGetEnumSelectList { get { throw null; } }
        internal static string HtmlHelper_WrongSelectDataType { get { throw null; } }
        internal static string PropertyOfTypeCannotBeNull { get { throw null; } }
        internal static string RemoteAttribute_NoUrlFound { get { throw null; } }
        internal static string RemoteAttribute_RemoteValidationFailed { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string SerializingTempData { get { throw null; } }
        internal static string TempDataProperties_InvalidType { get { throw null; } }
        internal static string TempDataProperties_PublicGetterSetter { get { throw null; } }
        internal static string TempData_CannotDeserializeType { get { throw null; } }
        internal static string TempData_CannotSerializeType { get { throw null; } }
        internal static string TemplatedExpander_PopulateValuesMustBeInvokedFirst { get { throw null; } }
        internal static string TemplatedExpander_ValueFactoryCannotReturnNull { get { throw null; } }
        internal static string TemplatedViewLocationExpander_NoReplacementToken { get { throw null; } }
        internal static string TemplateHelpers_NoTemplate { get { throw null; } }
        internal static string TemplateHelpers_TemplateLimitations { get { throw null; } }
        internal static string Templates_TypeMustImplementIEnumerable { get { throw null; } }
        internal static string TypeMethodMustReturnNotNullValue { get { throw null; } }
        internal static string TypeMustDeriveFromType { get { throw null; } }
        internal static string UnobtrusiveJavascript_ValidationParameterCannotBeEmpty { get { throw null; } }
        internal static string UnobtrusiveJavascript_ValidationParameterMustBeLegal { get { throw null; } }
        internal static string UnobtrusiveJavascript_ValidationTypeCannotBeEmpty { get { throw null; } }
        internal static string UnobtrusiveJavascript_ValidationTypeMustBeLegal { get { throw null; } }
        internal static string UnobtrusiveJavascript_ValidationTypeMustBeUnique { get { throw null; } }
        internal static string ValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated { get { throw null; } }
        internal static string ViewComponentResult_NameOrTypeMustBeSet { get { throw null; } }
        internal static string ViewComponent_AmbiguousMethods { get { throw null; } }
        internal static string ViewComponent_AmbiguousTypeMatch { get { throw null; } }
        internal static string ViewComponent_AmbiguousTypeMatch_Item { get { throw null; } }
        internal static string ViewComponent_AsyncMethod_ShouldReturnTask { get { throw null; } }
        internal static string ViewComponent_CannotFindComponent { get { throw null; } }
        internal static string ViewComponent_CannotFindMethod { get { throw null; } }
        internal static string ViewComponent_InvalidReturnValue { get { throw null; } }
        internal static string ViewComponent_IViewComponentFactory_ReturnedNull { get { throw null; } }
        internal static string ViewComponent_MustReturnValue { get { throw null; } }
        internal static string ViewComponent_SyncMethod_CannotReturnTask { get { throw null; } }
        internal static string ViewComponent_SyncMethod_ShouldReturnValue { get { throw null; } }
        internal static string ViewData_ModelCannotBeNull { get { throw null; } }
        internal static string ViewData_WrongTModelType { get { throw null; } }
        internal static string ViewEnginesAreRequired { get { throw null; } }
        internal static string ViewEngine_PartialViewNotFound { get { throw null; } }
        internal static string ViewEngine_ViewNotFound { get { throw null; } }
        internal static string FormatArgumentPropertyUnexpectedType(object p0, object p1, object p2) { throw null; }
        internal static string FormatCommon_PartialViewNotFound(object p0, object p1) { throw null; }
        internal static string FormatCommon_PropertyNotFound(object p0, object p1) { throw null; }
        internal static string FormatCreateModelExpression_NullModelMetadata(object p0, object p1) { throw null; }
        internal static string FormatDictionary_DuplicateKey(object p0) { throw null; }
        internal static string FormatExpressionHelper_InvalidIndexerExpression(object p0, object p1) { throw null; }
        internal static string FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(object p0, object p1, object p2, object p3, object p4) { throw null; }
        internal static string FormatHtmlHelper_MissingSelectData(object p0, object p1) { throw null; }
        internal static string FormatHtmlHelper_NullModelMetadata(object p0) { throw null; }
        internal static string FormatHtmlHelper_SelectExpressionNotEnumerable(object p0) { throw null; }
        internal static string FormatHtmlHelper_TypeNotSupported_ForGetEnumSelectList(object p0, object p1, object p2) { throw null; }
        internal static string FormatHtmlHelper_WrongSelectDataType(object p0, object p1, object p2) { throw null; }
        internal static string FormatPropertyOfTypeCannotBeNull(object p0, object p1) { throw null; }
        internal static string FormatRemoteAttribute_RemoteValidationFailed(object p0) { throw null; }
        internal static string FormatTempDataProperties_InvalidType(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatTempDataProperties_PublicGetterSetter(object p0, object p1, object p2) { throw null; }
        internal static string FormatTempData_CannotDeserializeType(object p0) { throw null; }
        internal static string FormatTempData_CannotSerializeType(object p0, object p1) { throw null; }
        internal static string FormatTemplatedExpander_PopulateValuesMustBeInvokedFirst(object p0, object p1) { throw null; }
        internal static string FormatTemplatedViewLocationExpander_NoReplacementToken(object p0) { throw null; }
        internal static string FormatTemplateHelpers_NoTemplate(object p0) { throw null; }
        internal static string FormatTemplates_TypeMustImplementIEnumerable(object p0, object p1, object p2) { throw null; }
        internal static string FormatTypeMethodMustReturnNotNullValue(object p0, object p1) { throw null; }
        internal static string FormatTypeMustDeriveFromType(object p0, object p1) { throw null; }
        internal static string FormatUnobtrusiveJavascript_ValidationParameterCannotBeEmpty(object p0) { throw null; }
        internal static string FormatUnobtrusiveJavascript_ValidationParameterMustBeLegal(object p0, object p1) { throw null; }
        internal static string FormatUnobtrusiveJavascript_ValidationTypeCannotBeEmpty(object p0) { throw null; }
        internal static string FormatUnobtrusiveJavascript_ValidationTypeMustBeLegal(object p0, object p1) { throw null; }
        internal static string FormatUnobtrusiveJavascript_ValidationTypeMustBeUnique(object p0) { throw null; }
        internal static string FormatValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated(object p0, object p1) { throw null; }
        internal static string FormatViewComponentResult_NameOrTypeMustBeSet(object p0, object p1) { throw null; }
        internal static string FormatViewComponent_AmbiguousMethods(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_AmbiguousTypeMatch(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_AmbiguousTypeMatch_Item(object p0, object p1) { throw null; }
        internal static string FormatViewComponent_AsyncMethod_ShouldReturnTask(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_CannotFindComponent(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatViewComponent_CannotFindMethod(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_InvalidReturnValue(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_IViewComponentFactory_ReturnedNull(object p0) { throw null; }
        internal static string FormatViewComponent_SyncMethod_CannotReturnTask(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewComponent_SyncMethod_ShouldReturnValue(object p0, object p1) { throw null; }
        internal static string FormatViewData_ModelCannotBeNull(object p0) { throw null; }
        internal static string FormatViewData_WrongTModelType(object p0, object p1) { throw null; }
        internal static string FormatViewEnginesAreRequired(object p0, object p1, object p2) { throw null; }
        internal static string FormatViewEngine_PartialViewNotFound(object p0, object p1) { throw null; }
        internal static string FormatViewEngine_ViewNotFound(object p0, object p1) { throw null; }
    }
    internal partial class ServerComponentInvocationSequence
    {
        public ServerComponentInvocationSequence() { }
        public System.Guid Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Next() { throw null; }
    }
    internal partial class ServerComponentSerializer
    {
        public ServerComponentSerializer(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider) { }
        internal System.Collections.Generic.IEnumerable<string> GetEpilogue(Microsoft.AspNetCore.Components.ServerComponentMarker record) { throw null; }
        internal System.Collections.Generic.IEnumerable<string> GetPreamble(Microsoft.AspNetCore.Components.ServerComponentMarker record) { throw null; }
        public Microsoft.AspNetCore.Components.ServerComponentMarker SerializeInvocation(Microsoft.AspNetCore.Mvc.ViewFeatures.ServerComponentInvocationSequence invocationId, System.Type type, bool prerendered) { throw null; }
    }
    internal partial class StaticComponentRenderer
    {
        public StaticComponentRenderer(System.Text.Encodings.Web.HtmlEncoder encoder) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<string>> PrerenderComponentAsync(Microsoft.AspNetCore.Components.ParameterView parameters, Microsoft.AspNetCore.Http.HttpContext httpContext, System.Type componentType) { throw null; }
    }
    internal partial class NullView : Microsoft.AspNetCore.Mvc.ViewEngines.IView
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.NullView Instance;
        public NullView() { }
        public string Path { get { throw null; } }
        public System.Threading.Tasks.Task RenderAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { throw null; }
    }
    internal partial class TemplateRenderer
    {
        public const string IEnumerableOfIFormFileName = "IEnumerable`IFormFile";
        public TemplateRenderer(Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, string templateName, bool readOnly) { }
        public static System.Collections.Generic.IEnumerable<string> GetTypeNames(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, System.Type fieldType) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Render() { throw null; }
    }
    internal static partial class FormatWeekHelper
    {
        public static string GetFormattedWeek(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { throw null; }
    }
    internal partial class UnsupportedJavaScriptRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public UnsupportedJavaScriptRuntime() { }
        public System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(string identifier, System.Threading.CancellationToken cancellationToken, object[] args) { throw null; }
        System.Threading.Tasks.ValueTask<TValue> Microsoft.JSInterop.IJSRuntime.InvokeAsync<TValue>(string identifier, object[] args) { throw null; }
    }
    public partial class ViewDataDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        internal ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider) { }
        internal System.Collections.Generic.IDictionary<string, object> Data { get { throw null; } }
    }
    internal static partial class ViewDataDictionaryFactory
    {
        public static System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> CreateFactory(System.Reflection.TypeInfo modelType) { throw null; }
        public static System.Func<Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> CreateNestedFactory(System.Reflection.TypeInfo modelType) { throw null; }
    }
    public partial class ViewDataDictionary<TModel> : Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary
    {
        internal ViewDataDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    internal partial class DefaultTempDataSerializer : Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer
    {
        public DefaultTempDataSerializer() { }
        public override bool CanSerializeType(System.Type type) { throw null; }
        public override System.Collections.Generic.IDictionary<string, object> Deserialize(byte[] value) { throw null; }
        public override byte[] Serialize(System.Collections.Generic.IDictionary<string, object> values) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal partial class SystemTextJsonHelper : Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper
    {
        public SystemTextJsonHelper(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> options) { }
        public Microsoft.AspNetCore.Html.IHtmlContent Serialize(object value) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcViewFeaturesMvcCoreBuilderExtensions
    {
        internal static void AddViewComponentApplicationPartsProviders(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        internal static void AddViewServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
    internal partial class MvcViewOptionsSetup
    {
        public MvcViewOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider) { }
        public MvcViewOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationOptions, Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcViewOptions options) { }
    }
    internal partial class TempDataMvcOptionsSetup
    {
        public TempDataMvcOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}
namespace Microsoft.AspNetCore.Components.Rendering
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct ComponentRenderedText
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ComponentRenderedText(int componentId, System.Collections.Generic.IEnumerable<string> tokens) { throw null; }
        public int ComponentId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> Tokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class HtmlRenderer : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        public HtmlRenderer(System.IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Func<string, string> htmlEncoder) : base (default(System.IServiceProvider), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public override Microsoft.AspNetCore.Components.Dispatcher Dispatcher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected override void HandleException(System.Exception exception) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Rendering.ComponentRenderedText> RenderComponentAsync(System.Type componentType, Microsoft.AspNetCore.Components.ParameterView initialParameters) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Rendering.ComponentRenderedText> RenderComponentAsync<TComponent>(Microsoft.AspNetCore.Components.ParameterView initialParameters) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        protected override System.Threading.Tasks.Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch renderBatch) { throw null; }
    }
}