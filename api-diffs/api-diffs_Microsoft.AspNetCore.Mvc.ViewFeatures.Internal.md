# Microsoft.AspNetCore.Mvc.ViewFeatures.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal {
 {
-    public class ArrayPoolBufferSource : ICharBufferSource {
 {
-        public ArrayPoolBufferSource(ArrayPool<char> pool);

-        public char[] Rent(int bufferSize);

-        public void Return(char[] buffer);

-    }
-    public class AutoValidateAntiforgeryTokenAuthorizationFilter : ValidateAntiforgeryTokenAuthorizationFilter {
 {
-        public AutoValidateAntiforgeryTokenAuthorizationFilter(IAntiforgery antiforgery, ILoggerFactory loggerFactory);

-        protected override bool ShouldValidate(AuthorizationFilterContext context);

-    }
-    public class CharArrayBufferSource : ICharBufferSource {
 {
-        public static readonly CharArrayBufferSource Instance;

-        public CharArrayBufferSource();

-        public char[] Rent(int bufferSize);

-        public void Return(char[] buffer);

-    }
-    public static class DefaultDisplayTemplates {
 {
-        public static IHtmlContent BooleanTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent CollectionTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent DecimalTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent EmailAddressTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent HiddenInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent HtmlTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent ObjectTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent StringTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent UrlTemplate(IHtmlHelper htmlHelper);

-    }
-    public static class DefaultEditorTemplates {
 {
-        public static IHtmlContent BooleanTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent CollectionTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent DateInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent DateTimeLocalInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent DateTimeOffsetTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent DecimalTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent EmailAddressInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent FileCollectionInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent FileInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent HiddenInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent MonthInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent MultilineTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent NumberInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent ObjectTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent PasswordTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent PhoneNumberInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent StringTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent TimeInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent UrlInputTemplate(IHtmlHelper htmlHelper);

-        public static IHtmlContent WeekInputTemplate(IHtmlHelper htmlHelper);

-    }
-    public class DynamicViewData : DynamicObject {
 {
-        public DynamicViewData(Func<ViewDataDictionary> viewDataFunc);

-        public override IEnumerable<string> GetDynamicMemberNames();

-        public override bool TryGetMember(GetMemberBinder binder, out object result);

-        public override bool TrySetMember(SetMemberBinder binder, object value);

-    }
-    public static class ExpressionHelper {
 {
-        public static string GetExpressionText(LambdaExpression expression);

-        public static string GetExpressionText(LambdaExpression expression, ExpressionTextCache expressionTextCache);

-        public static string GetExpressionText(string expression);

-        public static bool IsSingleArgumentIndexer(Expression expression);

-    }
-    public static class ExpressionMetadataProvider {
 {
-        public static ModelExplorer FromLambdaExpression<TModel, TResult>(Expression<Func<TModel, TResult>> expression, ViewDataDictionary<TModel> viewData, IModelMetadataProvider metadataProvider);

-        public static ModelExplorer FromStringExpression(string expression, ViewDataDictionary viewData, IModelMetadataProvider metadataProvider);

-    }
-    public class ExpressionTextCache {
 {
-        public ExpressionTextCache();

-        public ConcurrentDictionary<LambdaExpression, string> Entries { get; }

-    }
-    public class FormatWeekHelper {
 {
-        public FormatWeekHelper();

-        public static string GetFormattedWeek(ModelExplorer modelExplorer);

-    }
-    public interface ICharBufferSource {
 {
-        char[] Rent(int bufferSize);

-        void Return(char[] buffer);

-    }
-    public interface ISaveTempDataCallback : IFilterMetadata {
 {
-        void OnTempDataSaving(ITempDataDictionary tempData);

-    }
-    public interface IViewBufferScope {
 {
-        PagedBufferedTextWriter CreateWriter(TextWriter writer);

-        ViewBufferValue[] GetPage(int pageSize);

-        void ReturnSegment(ViewBufferValue[] segment);

-    }
-    public interface IViewDataValuesProviderFeature {
 {
-        void ProvideViewDataValues(ViewDataDictionary viewData);

-    }
-    public readonly struct LifecycleProperty {
 {
-        public LifecycleProperty(PropertyInfo propertyInfo, string key);

-        public string Key { get; }

-        public PropertyInfo PropertyInfo { get; }

-        public object GetValue(object instance);

-        public void SetValue(object instance, object value);

-    }
-    public class MemoryPoolViewBufferScope : IDisposable, IViewBufferScope {
 {
-        public static readonly int MinimumSize;

-        public MemoryPoolViewBufferScope(ArrayPool<ViewBufferValue> viewBufferPool, ArrayPool<char> charPool);

-        public PagedBufferedTextWriter CreateWriter(TextWriter writer);

-        public void Dispose();

-        public ViewBufferValue[] GetPage(int pageSize);

-        public void ReturnSegment(ViewBufferValue[] segment);

-    }
-    public class MvcViewOptionsSetup : IConfigureOptions<MvcViewOptions> {
 {
-        public MvcViewOptionsSetup(IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, IValidationAttributeAdapterProvider validationAttributeAdapterProvider);

-        public MvcViewOptionsSetup(IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationOptions, IValidationAttributeAdapterProvider validationAttributeAdapterProvider, IStringLocalizerFactory stringLocalizerFactory);

-        public void Configure(MvcViewOptions options);

-    }
-    public static class NameAndIdProvider {
 {
-        public static string CreateSanitizedId(ViewContext viewContext, string fullName, string invalidCharReplacement);

-        public static void GenerateId(ViewContext viewContext, TagBuilder tagBuilder, string fullName, string invalidCharReplacement);

-        public static string GetFullHtmlFieldName(ViewContext viewContext, string expression);

-    }
-    public class NullView : IView {
 {
-        public static readonly NullView Instance;

-        public NullView();

-        public string Path { get; }

-        public Task RenderAsync(ViewContext context);

-    }
-    public class PagedBufferedTextWriter : TextWriter {
 {
-        public PagedBufferedTextWriter(ArrayPool<char> pool, TextWriter inner);

-        public override Encoding Encoding { get; }

-        protected override void Dispose(bool disposing);

-        public override void Flush();

-        public override Task FlushAsync();

-        public override void Write(char value);

-        public override void Write(char[] buffer);

-        public override void Write(char[] buffer, int index, int count);

-        public override void Write(string value);

-        public override Task WriteAsync(char value);

-        public override Task WriteAsync(char[] buffer, int index, int count);

-        public override Task WriteAsync(string value);

-    }
-    public class PagedCharBuffer : IDisposable {
 {
-        public const int PageSize = 1024;

-        public PagedCharBuffer(ICharBufferSource bufferSource);

-        public ICharBufferSource BufferSource { get; }

-        public int Length { get; }

-        public List<char[]> Pages { get; }

-        public void Append(char value);

-        public void Append(char[] buffer, int index, int count);

-        public void Append(string value);

-        public void Clear();

-        public void Dispose();

-    }
-    public class SaveTempDataFilter : IFilterMetadata, IResourceFilter, IResultFilter {
 {
-        public SaveTempDataFilter(ITempDataDictionaryFactory factory);

-        public void OnResourceExecuted(ResourceExecutedContext context);

-        public void OnResourceExecuting(ResourceExecutingContext context);

-        public void OnResultExecuted(ResultExecutedContext context);

-        public void OnResultExecuting(ResultExecutingContext context);

-    }
-    public abstract class SaveTempDataPropertyFilterBase : IFilterMetadata, ISaveTempDataCallback {
 {
-        protected readonly ITempDataDictionaryFactory _tempDataFactory;

-        public SaveTempDataPropertyFilterBase(ITempDataDictionaryFactory tempDataFactory);

-        public IDictionary<PropertyInfo, object> OriginalValues { get; }

-        public IReadOnlyList<LifecycleProperty> Properties { get; set; }

-        public object Subject { get; set; }

-        public static IReadOnlyList<LifecycleProperty> GetTempDataProperties(Type type, MvcViewOptions viewOptions);

-        public void OnTempDataSaving(ITempDataDictionary tempData);

-        protected void SetPropertyValues(ITempDataDictionary tempData);

-    }
-    public class TempDataMvcOptionsSetup : IConfigureOptions<MvcOptions> {
 {
-        public TempDataMvcOptionsSetup();

-        public void Configure(MvcOptions options);

-    }
-    public class TempDataSerializer {
 {
-        public TempDataSerializer();

-        public static bool CanSerializeType(Type typeToSerialize, out string errorMessage);

-        public IDictionary<string, object> Deserialize(byte[] value);

-        public void EnsureObjectCanBeSerialized(object item);

-        public byte[] Serialize(IDictionary<string, object> values);

-    }
-    public class TemplateBuilder {
 {
-        public TemplateBuilder(IViewEngine viewEngine, IViewBufferScope bufferScope, ViewContext viewContext, ViewDataDictionary viewData, ModelExplorer modelExplorer, string htmlFieldName, string templateName, bool readOnly, object additionalViewData);

-        public IHtmlContent Build();

-    }
-    public class TemplateRenderer {
 {
-        public const string IEnumerableOfIFormFileName = "IEnumerable`IFormFile";

-        public TemplateRenderer(IViewEngine viewEngine, IViewBufferScope bufferScope, ViewContext viewContext, ViewDataDictionary viewData, string templateName, bool readOnly);

-        public static IEnumerable<string> GetTypeNames(ModelMetadata modelMetadata, Type fieldType);

-        public IHtmlContent Render();

-    }
-    public class ValidateAntiforgeryTokenAuthorizationFilter : IAntiforgeryPolicy, IAsyncAuthorizationFilter, IFilterMetadata {
 {
-        public ValidateAntiforgeryTokenAuthorizationFilter(IAntiforgery antiforgery, ILoggerFactory loggerFactory);

-        public Task OnAuthorizationAsync(AuthorizationFilterContext context);

-        protected virtual bool ShouldValidate(AuthorizationFilterContext context);

-    }
-    public static class ValidationHelpers {
 {
-        public static string GetModelErrorMessageOrDefault(ModelError modelError);

-        public static string GetModelErrorMessageOrDefault(ModelError modelError, ModelStateEntry containingEntry, ModelExplorer modelExplorer);

-        public static IList<ModelStateEntry> GetModelStateList(ViewDataDictionary viewData, bool excludePropertyErrors);

-    }
-    public class ViewBuffer : IHtmlContent, IHtmlContentBuilder, IHtmlContentContainer {
 {
-        public static readonly int PartialViewPageSize;

-        public static readonly int TagHelperPageSize;

-        public static readonly int ViewComponentPageSize;

-        public static readonly int ViewPageSize;

-        public ViewBuffer(IViewBufferScope bufferScope, string name, int pageSize);

-        public int Count { get; }

-        public ViewBufferPage this[int index] { get; }

-        public IHtmlContentBuilder Append(string unencoded);

-        public IHtmlContentBuilder AppendHtml(IHtmlContent content);

-        public IHtmlContentBuilder AppendHtml(string encoded);

-        public IHtmlContentBuilder Clear();

-        public void CopyTo(IHtmlContentBuilder destination);

-        public void MoveTo(IHtmlContentBuilder destination);

-        public void WriteTo(TextWriter writer, HtmlEncoder encoder);

-        public Task WriteToAsync(TextWriter writer, HtmlEncoder encoder);

-    }
-    public class ViewBufferPage {
 {
-        public ViewBufferPage(ViewBufferValue[] buffer);

-        public ViewBufferValue[] Buffer { get; }

-        public int Capacity { get; }

-        public int Count { get; set; }

-        public bool IsFull { get; }

-        public void Append(ViewBufferValue value);

-    }
-    public class ViewBufferTextWriter : TextWriter {
 {
-        public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding);

-        public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding, HtmlEncoder htmlEncoder, TextWriter inner);

-        public ViewBuffer Buffer { get; }

-        public override Encoding Encoding { get; }

-        public bool IsBuffering { get; private set; }

-        public override void Flush();

-        public override Task FlushAsync();

-        public void Write(IHtmlContent value);

-        public void Write(IHtmlContentContainer value);

-        public override void Write(char value);

-        public override void Write(char[] buffer, int index, int count);

-        public override void Write(object value);

-        public override void Write(string value);

-        public override Task WriteAsync(char value);

-        public override Task WriteAsync(char[] buffer, int index, int count);

-        public override Task WriteAsync(string value);

-        public override void WriteLine();

-        public override void WriteLine(object value);

-        public override void WriteLine(string value);

-        public override Task WriteLineAsync();

-        public override Task WriteLineAsync(char value);

-        public override Task WriteLineAsync(char[] value, int start, int offset);

-        public override Task WriteLineAsync(string value);

-    }
-    public readonly struct ViewBufferValue {
 {
-        public ViewBufferValue(IHtmlContent content);

-        public ViewBufferValue(string value);

-        public object Value { get; }

-    }
-    public class ViewComponentInvokerCache {
 {
-        public ViewComponentInvokerCache(IViewComponentDescriptorCollectionProvider collectionProvider);

-    }
-    public static class ViewDataAttributePropertyProvider {
 {
-        public static IReadOnlyList<LifecycleProperty> GetViewDataProperties(Type type);

-    }
-    public static class ViewDataDictionaryFactory {
 {
-        public static Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> CreateFactory(TypeInfo modelType);

-        public static Func<ViewDataDictionary, ViewDataDictionary> CreateNestedFactory(TypeInfo modelType);

-    }
-}
```

