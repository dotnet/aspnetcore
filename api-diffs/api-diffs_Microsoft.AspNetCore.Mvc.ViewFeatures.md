# Microsoft.AspNetCore.Mvc.ViewFeatures

``` diff
 namespace Microsoft.AspNetCore.Mvc.ViewFeatures {
     public static class AntiforgeryExtensions {
         public static IHtmlContent GetHtml(this IAntiforgery antiforgery, HttpContext httpContext);
     }
     public class AttributeDictionary : ICollection<KeyValuePair<string, string>>, IDictionary<string, string>, IEnumerable, IEnumerable<KeyValuePair<string, string>>, IReadOnlyCollection<KeyValuePair<string, string>>, IReadOnlyDictionary<string, string> {
         public AttributeDictionary();
         public int Count { get; }
         public bool IsReadOnly { get; }
         public ICollection<string> Keys { get; }
         IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<System.String,System.String>.Keys { get; }
         IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<System.String,System.String>.Values { get; }
         public string this[string key] { get; set; }
         public ICollection<string> Values { get; }
         public void Add(KeyValuePair<string, string> item);
         public void Add(string key, string value);
         public void Clear();
         public bool Contains(KeyValuePair<string, string> item);
         public bool ContainsKey(string key);
         public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex);
         public AttributeDictionary.Enumerator GetEnumerator();
         public bool Remove(KeyValuePair<string, string> item);
         public bool Remove(string key);
         IEnumerator<KeyValuePair<string, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.String>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(string key, out string value);
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, string>> {
             public Enumerator(AttributeDictionary attributes);
             public KeyValuePair<string, string> Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
     }
     public class CookieTempDataProvider : ITempDataProvider {
         public static readonly string CookieName;
-        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider, ILoggerFactory loggerFactory, IOptions<CookieTempDataProviderOptions> options);

+        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider, ILoggerFactory loggerFactory, IOptions<CookieTempDataProviderOptions> options, TempDataSerializer tempDataSerializer);
         public IDictionary<string, object> LoadTempData(HttpContext context);
         public void SaveTempData(HttpContext context, IDictionary<string, object> values);
     }
     public class DefaultHtmlGenerator : IHtmlGenerator {
         public DefaultHtmlGenerator(IAntiforgery antiforgery, IOptions<MvcViewOptions> optionsAccessor, IModelMetadataProvider metadataProvider, IUrlHelperFactory urlHelperFactory, HtmlEncoder htmlEncoder, ValidationHtmlAttributeProvider validationAttributeProvider);
         protected bool AllowRenderingMaxLengthAttribute { get; }
         public string IdAttributeDotReplacement { get; }
         protected virtual void AddMaxLengthAttribute(ViewDataDictionary viewData, TagBuilder tagBuilder, ModelExplorer modelExplorer, string expression);
         protected virtual void AddPlaceholderAttribute(ViewDataDictionary viewData, TagBuilder tagBuilder, ModelExplorer modelExplorer, string expression);
         protected virtual void AddValidationAttributes(ViewContext viewContext, TagBuilder tagBuilder, ModelExplorer modelExplorer, string expression);
         public string Encode(object value);
         public string Encode(string value);
         public string FormatValue(object value, string format);
         public virtual TagBuilder GenerateActionLink(ViewContext viewContext, string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         public virtual IHtmlContent GenerateAntiforgery(ViewContext viewContext);
         public virtual TagBuilder GenerateCheckBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, Nullable<bool> isChecked, object htmlAttributes);
         public virtual TagBuilder GenerateForm(ViewContext viewContext, string actionName, string controllerName, object routeValues, string method, object htmlAttributes);
         protected virtual TagBuilder GenerateFormCore(ViewContext viewContext, string action, string method, object htmlAttributes);
         public IHtmlContent GenerateGroupsAndOptions(string optionLabel, IEnumerable<SelectListItem> selectList);
         public virtual TagBuilder GenerateHidden(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes);
         public virtual TagBuilder GenerateHiddenForCheckbox(ViewContext viewContext, ModelExplorer modelExplorer, string expression);
         protected virtual TagBuilder GenerateInput(ViewContext viewContext, InputType inputType, ModelExplorer modelExplorer, string expression, object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format, IDictionary<string, object> htmlAttributes);
         public virtual TagBuilder GenerateLabel(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes);
         protected virtual TagBuilder GenerateLink(string linkText, string url, object htmlAttributes);
         public virtual TagBuilder GeneratePageForm(ViewContext viewContext, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes);
         public virtual TagBuilder GeneratePageLink(ViewContext viewContext, string linkText, string pageName, string pageHandler, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         public virtual TagBuilder GeneratePassword(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, object htmlAttributes);
         public virtual TagBuilder GenerateRadioButton(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, Nullable<bool> isChecked, object htmlAttributes);
         public TagBuilder GenerateRouteForm(ViewContext viewContext, string routeName, object routeValues, string method, object htmlAttributes);
         public virtual TagBuilder GenerateRouteLink(ViewContext viewContext, string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
         public TagBuilder GenerateSelect(ViewContext viewContext, ModelExplorer modelExplorer, string optionLabel, string expression, IEnumerable<SelectListItem> selectList, bool allowMultiple, object htmlAttributes);
         public virtual TagBuilder GenerateSelect(ViewContext viewContext, ModelExplorer modelExplorer, string optionLabel, string expression, IEnumerable<SelectListItem> selectList, ICollection<string> currentValues, bool allowMultiple, object htmlAttributes);
         public virtual TagBuilder GenerateTextArea(ViewContext viewContext, ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes);
         public virtual TagBuilder GenerateTextBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes);
         public virtual TagBuilder GenerateValidationMessage(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes);
         public virtual TagBuilder GenerateValidationSummary(ViewContext viewContext, bool excludePropertyErrors, string message, string headerTag, object htmlAttributes);
         public virtual ICollection<string> GetCurrentValues(ViewContext viewContext, ModelExplorer modelExplorer, string expression, bool allowMultiple);
     }
     public static class DefaultHtmlGeneratorExtensions {
         public static TagBuilder GenerateForm(this IHtmlGenerator generator, ViewContext viewContext, string actionName, string controllerName, string fragment, object routeValues, string method, object htmlAttributes);
         public static TagBuilder GenerateRouteForm(this IHtmlGenerator generator, ViewContext viewContext, string routeName, object routeValues, string fragment, string method, object htmlAttributes);
     }
     public class DefaultValidationHtmlAttributeProvider : ValidationHtmlAttributeProvider {
-        public DefaultValidationHtmlAttributeProvider(IOptions<MvcViewOptions> optionsAccessor, IModelMetadataProvider metadataProvider, ClientValidatorCache clientValidatorCache);

+        public DefaultValidationHtmlAttributeProvider(IOptions<MvcViewOptions> optionsAccessor, IModelMetadataProvider metadataProvider, ClientValidatorCache clientValidatorCache);
         public override void AddValidationAttributes(ViewContext viewContext, ModelExplorer modelExplorer, IDictionary<string, string> attributes);
     }
     public class FormContext {
         public FormContext();
         public bool CanRenderAtEndOfForm { get; set; }
         public IList<IHtmlContent> EndOfFormContent { get; }
         public IDictionary<string, object> FormData { get; }
         public bool HasAntiforgeryToken { get; set; }
         public bool HasEndOfFormContent { get; }
         public bool HasFormData { get; }
         public bool RenderedField(string fieldName);
         public void RenderedField(string fieldName, bool value);
     }
     public class HtmlHelper : IHtmlHelper, IViewContextAware {
         public static readonly string ValidationInputCssClassName;
         public static readonly string ValidationInputValidCssClassName;
         public static readonly string ValidationMessageCssClassName;
         public static readonly string ValidationMessageValidCssClassName;
         public static readonly string ValidationSummaryCssClassName;
         public static readonly string ValidationSummaryValidCssClassName;
+        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder);
-        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder);

         public Html5DateRenderingMode Html5DateRenderingMode { get; set; }
         public string IdAttributeDotReplacement { get; }
         public IModelMetadataProvider MetadataProvider { get; }
         public ITempDataDictionary TempData { get; }
         public UrlEncoder UrlEncoder { get; }
         public dynamic ViewBag { get; }
         public ViewContext ViewContext { get; private set; }
         public ViewDataDictionary ViewData { get; }
         public IHtmlContent ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes);
         public IHtmlContent AntiForgeryToken();
         public MvcForm BeginForm(string actionName, string controllerName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         public MvcForm BeginRouteForm(string routeName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         public IHtmlContent CheckBox(string expression, Nullable<bool> isChecked, object htmlAttributes);
         public virtual void Contextualize(ViewContext viewContext);
         protected virtual MvcForm CreateForm();
         public IHtmlContent Display(string expression, string templateName, string htmlFieldName, object additionalViewData);
         public string DisplayName(string expression);
         public string DisplayText(string expression);
         public IHtmlContent DropDownList(string expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes);
         public IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);
         public string Encode(object value);
         public string Encode(string value);
         public void EndForm();
         public string FormatValue(object value, string format);
         protected virtual IHtmlContent GenerateCheckBox(ModelExplorer modelExplorer, string expression, Nullable<bool> isChecked, object htmlAttributes);
         protected virtual IHtmlContent GenerateDisplay(ModelExplorer modelExplorer, string htmlFieldName, string templateName, object additionalViewData);
         protected virtual string GenerateDisplayName(ModelExplorer modelExplorer, string expression);
         protected virtual string GenerateDisplayText(ModelExplorer modelExplorer);
         protected IHtmlContent GenerateDropDown(ModelExplorer modelExplorer, string expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes);
         protected virtual IHtmlContent GenerateEditor(ModelExplorer modelExplorer, string htmlFieldName, string templateName, object additionalViewData);
         protected virtual MvcForm GenerateForm(string actionName, string controllerName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         protected virtual IHtmlContent GenerateHidden(ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes);
         protected virtual string GenerateId(string expression);
         public string GenerateIdFromName(string fullName);
         protected virtual IHtmlContent GenerateLabel(ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes);
         protected IHtmlContent GenerateListBox(ModelExplorer modelExplorer, string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         protected virtual string GenerateName(string expression);
         protected virtual IHtmlContent GeneratePassword(ModelExplorer modelExplorer, string expression, object value, object htmlAttributes);
         protected virtual IHtmlContent GenerateRadioButton(ModelExplorer modelExplorer, string expression, object value, Nullable<bool> isChecked, object htmlAttributes);
         protected virtual MvcForm GenerateRouteForm(string routeName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         protected virtual IHtmlContent GenerateTextArea(ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes);
         protected virtual IHtmlContent GenerateTextBox(ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes);
         protected virtual IHtmlContent GenerateValidationMessage(ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes);
         protected virtual IHtmlContent GenerateValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag);
         protected virtual string GenerateValue(string expression, object value, string format, bool useViewData);
         protected virtual IEnumerable<SelectListItem> GetEnumSelectList(ModelMetadata metadata);
         public IEnumerable<SelectListItem> GetEnumSelectList(Type enumType);
-        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct, ValueType;
+        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;
         public static string GetFormMethodString(FormMethod method);
         public IHtmlContent Hidden(string expression, object value, object htmlAttributes);
         public string Id(string expression);
         public IHtmlContent Label(string expression, string labelText, object htmlAttributes);
         public IHtmlContent ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         public string Name(string expression);
         public static IDictionary<string, object> ObjectToDictionary(object value);
         public Task<IHtmlContent> PartialAsync(string partialViewName, object model, ViewDataDictionary viewData);
         public IHtmlContent Password(string expression, object value, object htmlAttributes);
         public IHtmlContent RadioButton(string expression, object value, Nullable<bool> isChecked, object htmlAttributes);
         public IHtmlContent Raw(object value);
         public IHtmlContent Raw(string value);
         public Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData);
         protected virtual Task RenderPartialCoreAsync(string partialViewName, object model, ViewDataDictionary viewData, TextWriter writer);
         public IHtmlContent RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
         public IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes);
         public IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes);
         public IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag);
         public IHtmlContent ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag);
         public string Value(string expression, string format);
     }
     public class HtmlHelper<TModel> : HtmlHelper, IHtmlHelper, IHtmlHelper<TModel> {
+        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder, ModelExpressionProvider modelExpressionProvider);
-        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder, ExpressionTextCache expressionTextCache);

         public new ViewDataDictionary<TModel> ViewData { get; private set; }
         public IHtmlContent CheckBoxFor(Expression<Func<TModel, bool>> expression, object htmlAttributes);
         public override void Contextualize(ViewContext viewContext);
         public IHtmlContent DisplayFor<TResult>(Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
         public string DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression);
         public string DisplayNameForInnerType<TModelItem, TResult>(Expression<Func<TModelItem, TResult>> expression);
         public string DisplayTextFor<TResult>(Expression<Func<TModel, TResult>> expression);
         public IHtmlContent DropDownListFor<TResult>(Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes);
         public IHtmlContent EditorFor<TResult>(Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
         protected string GetExpressionName<TResult>(Expression<Func<TModel, TResult>> expression);
         protected ModelExplorer GetModelExplorer<TResult>(Expression<Func<TModel, TResult>> expression);
         public IHtmlContent HiddenFor<TResult>(Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         public string IdFor<TResult>(Expression<Func<TModel, TResult>> expression);
         public IHtmlContent LabelFor<TResult>(Expression<Func<TModel, TResult>> expression, string labelText, object htmlAttributes);
         public IHtmlContent ListBoxFor<TResult>(Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         public string NameFor<TResult>(Expression<Func<TModel, TResult>> expression);
         public IHtmlContent PasswordFor<TResult>(Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         public IHtmlContent RadioButtonFor<TResult>(Expression<Func<TModel, TResult>> expression, object value, object htmlAttributes);
         public IHtmlContent TextAreaFor<TResult>(Expression<Func<TModel, TResult>> expression, int rows, int columns, object htmlAttributes);
         public IHtmlContent TextBoxFor<TResult>(Expression<Func<TModel, TResult>> expression, string format, object htmlAttributes);
         public IHtmlContent ValidationMessageFor<TResult>(Expression<Func<TModel, TResult>> expression, string message, object htmlAttributes, string tag);
         public string ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string format);
     }
     public class HtmlHelperOptions {
         public HtmlHelperOptions();
         public bool ClientValidationEnabled { get; set; }
         public Html5DateRenderingMode Html5DateRenderingMode { get; set; }
         public string IdAttributeDotReplacement { get; set; }
         public string ValidationMessageElement { get; set; }
         public string ValidationSummaryMessageElement { get; set; }
     }
     public interface IAntiforgeryPolicy : IFilterMetadata
     public interface IFileVersionProvider {
         string AddFileVersionToPath(PathString requestPathBase, string path);
     }
     public interface IHtmlGenerator {
         string IdAttributeDotReplacement { get; }
         string Encode(object value);
         string Encode(string value);
         string FormatValue(object value, string format);
         TagBuilder GenerateActionLink(ViewContext viewContext, string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         IHtmlContent GenerateAntiforgery(ViewContext viewContext);
         TagBuilder GenerateCheckBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, Nullable<bool> isChecked, object htmlAttributes);
         TagBuilder GenerateForm(ViewContext viewContext, string actionName, string controllerName, object routeValues, string method, object htmlAttributes);
         IHtmlContent GenerateGroupsAndOptions(string optionLabel, IEnumerable<SelectListItem> selectList);
         TagBuilder GenerateHidden(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes);
         TagBuilder GenerateHiddenForCheckbox(ViewContext viewContext, ModelExplorer modelExplorer, string expression);
         TagBuilder GenerateLabel(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string labelText, object htmlAttributes);
         TagBuilder GeneratePageForm(ViewContext viewContext, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes);
         TagBuilder GeneratePageLink(ViewContext viewContext, string linkText, string pageName, string pageHandler, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         TagBuilder GeneratePassword(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, object htmlAttributes);
         TagBuilder GenerateRadioButton(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, Nullable<bool> isChecked, object htmlAttributes);
         TagBuilder GenerateRouteForm(ViewContext viewContext, string routeName, object routeValues, string method, object htmlAttributes);
         TagBuilder GenerateRouteLink(ViewContext viewContext, string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
         TagBuilder GenerateSelect(ViewContext viewContext, ModelExplorer modelExplorer, string optionLabel, string expression, IEnumerable<SelectListItem> selectList, bool allowMultiple, object htmlAttributes);
         TagBuilder GenerateSelect(ViewContext viewContext, ModelExplorer modelExplorer, string optionLabel, string expression, IEnumerable<SelectListItem> selectList, ICollection<string> currentValues, bool allowMultiple, object htmlAttributes);
         TagBuilder GenerateTextArea(ViewContext viewContext, ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes);
         TagBuilder GenerateTextBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes);
         TagBuilder GenerateValidationMessage(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string message, string tag, object htmlAttributes);
         TagBuilder GenerateValidationSummary(ViewContext viewContext, bool excludePropertyErrors, string message, string headerTag, object htmlAttributes);
         ICollection<string> GetCurrentValues(ViewContext viewContext, ModelExplorer modelExplorer, string expression, bool allowMultiple);
     }
     public interface IKeepTempDataResult : IActionResult
     public interface IModelExpressionProvider {
         ModelExpression CreateModelExpression<TModel, TValue>(ViewDataDictionary<TModel> viewData, Expression<Func<TModel, TValue>> expression);
     }
     public enum InputType {
         CheckBox = 0,
         Hidden = 1,
         Password = 2,
         Radio = 3,
         Text = 4,
     }
     public interface ITempDataDictionary : ICollection<KeyValuePair<string, object>>, IDictionary<string, object>, IEnumerable, IEnumerable<KeyValuePair<string, object>> {
         void Keep();
         void Keep(string key);
         void Load();
         object Peek(string key);
         void Save();
     }
     public interface ITempDataDictionaryFactory {
         ITempDataDictionary GetTempData(HttpContext context);
     }
     public interface ITempDataProvider {
         IDictionary<string, object> LoadTempData(HttpContext context);
         void SaveTempData(HttpContext context, IDictionary<string, object> values);
     }
     public interface IViewContextAware {
         void Contextualize(ViewContext viewContext);
     }
-    public class JsonHelper : IJsonHelper {
 {
-        public JsonHelper(JsonOutputFormatter jsonOutputFormatter, ArrayPool<char> charPool);

-        public IHtmlContent Serialize(object value);

-        public IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings);

-    }
     public class ModelExplorer {
         public ModelExplorer(IModelMetadataProvider metadataProvider, ModelMetadata metadata, object model);
         public ModelExplorer(IModelMetadataProvider metadataProvider, ModelExplorer container, ModelMetadata metadata, Func<object, object> modelAccessor);
         public ModelExplorer(IModelMetadataProvider metadataProvider, ModelExplorer container, ModelMetadata metadata, object model);
         public ModelExplorer Container { get; }
         public ModelMetadata Metadata { get; }
         public object Model { get; }
         public Type ModelType { get; }
         public IEnumerable<ModelExplorer> Properties { get; }
         public ModelExplorer GetExplorerForExpression(ModelMetadata metadata, Func<object, object> modelAccessor);
         public ModelExplorer GetExplorerForExpression(ModelMetadata metadata, object model);
         public ModelExplorer GetExplorerForExpression(Type modelType, Func<object, object> modelAccessor);
         public ModelExplorer GetExplorerForExpression(Type modelType, object model);
         public ModelExplorer GetExplorerForModel(object model);
         public ModelExplorer GetExplorerForProperty(string name);
         public ModelExplorer GetExplorerForProperty(string name, Func<object, object> modelAccessor);
         public ModelExplorer GetExplorerForProperty(string name, object model);
     }
     public static class ModelExplorerExtensions {
         public static string GetSimpleDisplayText(this ModelExplorer modelExplorer);
     }
     public sealed class ModelExpression {
         public ModelExpression(string name, ModelExplorer modelExplorer);
         public ModelMetadata Metadata { get; }
         public object Model { get; }
         public ModelExplorer ModelExplorer { get; }
         public string Name { get; }
     }
     public class ModelExpressionProvider : IModelExpressionProvider {
+        public ModelExpressionProvider(IModelMetadataProvider modelMetadataProvider);
-        public ModelExpressionProvider(IModelMetadataProvider modelMetadataProvider, ExpressionTextCache expressionTextCache);

         public ModelExpression CreateModelExpression<TModel, TValue>(ViewDataDictionary<TModel> viewData, Expression<Func<TModel, TValue>> expression);
+        public ModelExpression CreateModelExpression<TModel>(ViewDataDictionary<TModel> viewData, string expression);
+        public string GetExpressionText<TModel, TValue>(Expression<Func<TModel, TValue>> expression);
     }
     public static class ModelMetadataProviderExtensions {
         public static ModelExplorer GetModelExplorerForType(this IModelMetadataProvider provider, Type modelType, object model);
     }
     public class PartialViewResultExecutor : ViewExecutor, IActionResultExecutor<PartialViewResult> {
+        public PartialViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);
-        public PartialViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);

         protected ILogger Logger { get; }
         public virtual Task ExecuteAsync(ActionContext context, PartialViewResult result);
         public virtual Task ExecuteAsync(ActionContext actionContext, IView view, PartialViewResult viewResult);
         public virtual ViewEngineResult FindView(ActionContext actionContext, PartialViewResult viewResult);
     }
     public class SaveTempDataAttribute : Attribute, IFilterFactory, IFilterMetadata, IOrderedFilter {
         public SaveTempDataAttribute();
         public bool IsReusable { get; }
         public int Order { get; set; }
         public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public class SessionStateTempDataProvider : ITempDataProvider {
-        public SessionStateTempDataProvider();

+        public SessionStateTempDataProvider(TempDataSerializer tempDataSerializer);
         public virtual IDictionary<string, object> LoadTempData(HttpContext context);
         public virtual void SaveTempData(HttpContext context, IDictionary<string, object> values);
     }
     public class StringHtmlContent : IHtmlContent {
         public StringHtmlContent(string input);
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public class TempDataDictionary : ICollection<KeyValuePair<string, object>>, IDictionary<string, object>, IEnumerable, IEnumerable<KeyValuePair<string, object>>, ITempDataDictionary {
         public TempDataDictionary(HttpContext context, ITempDataProvider provider);
         public int Count { get; }
         public ICollection<string> Keys { get; }
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.IsReadOnly { get; }
         public object this[string key] { get; set; }
         public ICollection<object> Values { get; }
         public void Add(string key, object value);
         public void Clear();
         public bool ContainsKey(string key);
         public bool ContainsValue(object value);
         public IEnumerator<KeyValuePair<string, object>> GetEnumerator();
         public void Keep();
         public void Keep(string key);
         public void Load();
         public object Peek(string key);
         public bool Remove(string key);
         public void Save();
         void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Add(KeyValuePair<string, object> keyValuePair);
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Contains(KeyValuePair<string, object> keyValuePair);
         void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.CopyTo(KeyValuePair<string, object>[] array, int index);
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Remove(KeyValuePair<string, object> keyValuePair);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(string key, out object value);
     }
     public class TempDataDictionaryFactory : ITempDataDictionaryFactory {
         public TempDataDictionaryFactory(ITempDataProvider provider);
         public ITempDataDictionary GetTempData(HttpContext context);
     }
     public class TemplateInfo {
         public TemplateInfo();
         public TemplateInfo(TemplateInfo original);
         public object FormattedModelValue { get; set; }
         public string HtmlFieldPrefix { get; set; }
         public int TemplateDepth { get; }
         public bool AddVisited(object value);
         public string GetFullHtmlFieldName(string partialFieldName);
         public bool Visited(ModelExplorer modelExplorer);
     }
     public delegate bool TryGetValueDelegate(object dictionary, string key, out object value);
     public static class TryGetValueProvider {
         public static TryGetValueDelegate CreateInstance(Type targetType);
     }
     public abstract class ValidationHtmlAttributeProvider {
         protected ValidationHtmlAttributeProvider();
         public virtual void AddAndTrackValidationAttributes(ViewContext viewContext, ModelExplorer modelExplorer, string expression, IDictionary<string, string> attributes);
         public abstract void AddValidationAttributes(ViewContext viewContext, ModelExplorer modelExplorer, IDictionary<string, string> attributes);
     }
     public class ViewComponentResultExecutor : IActionResultExecutor<ViewComponentResult> {
         public ViewComponentResultExecutor(IOptions<MvcViewOptions> mvcHelperOptions, ILoggerFactory loggerFactory, HtmlEncoder htmlEncoder, IModelMetadataProvider modelMetadataProvider, ITempDataDictionaryFactory tempDataDictionaryFactory);
+        public ViewComponentResultExecutor(IOptions<MvcViewOptions> mvcHelperOptions, ILoggerFactory loggerFactory, HtmlEncoder htmlEncoder, IModelMetadataProvider modelMetadataProvider, ITempDataDictionaryFactory tempDataDictionaryFactory, IHttpResponseStreamWriterFactory writerFactory);
         public virtual Task ExecuteAsync(ActionContext context, ViewComponentResult result);
     }
     public class ViewContextAttribute : Attribute {
         public ViewContextAttribute();
     }
     public class ViewDataDictionary : ICollection<KeyValuePair<string, object>>, IDictionary<string, object>, IEnumerable, IEnumerable<KeyValuePair<string, object>> {
         public ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState);
         protected ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState, Type declaredModelType);
         protected ViewDataDictionary(IModelMetadataProvider metadataProvider, Type declaredModelType);
         public ViewDataDictionary(ViewDataDictionary source);
         protected ViewDataDictionary(ViewDataDictionary source, object model, Type declaredModelType);
         protected ViewDataDictionary(ViewDataDictionary source, Type declaredModelType);
         public int Count { get; }
         public bool IsReadOnly { get; }
         public ICollection<string> Keys { get; }
         public object Model { get; set; }
         public ModelExplorer ModelExplorer { get; set; }
         public ModelMetadata ModelMetadata { get; }
         public ModelStateDictionary ModelState { get; }
         public TemplateInfo TemplateInfo { get; }
         public object this[string index] { get; set; }
         public ICollection<object> Values { get; }
         public void Add(KeyValuePair<string, object> item);
         public void Add(string key, object value);
         public void Clear();
         public bool Contains(KeyValuePair<string, object> item);
         public bool ContainsKey(string key);
         public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex);
         public object Eval(string expression);
         public string Eval(string expression, string format);
         public static string FormatValue(object value, string format);
         public ViewDataInfo GetViewDataInfo(string expression);
         public bool Remove(KeyValuePair<string, object> item);
         public bool Remove(string key);
         protected virtual void SetModel(object value);
         IEnumerator<KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(string key, out object value);
     }
     public class ViewDataDictionary<TModel> : ViewDataDictionary {
         public ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState);
         public ViewDataDictionary(ViewDataDictionary source);
         public ViewDataDictionary(ViewDataDictionary source, object model);
         public new TModel Model { get; set; }
     }
     public class ViewDataDictionaryAttribute : Attribute {
         public ViewDataDictionaryAttribute();
     }
-    public class ViewDataDictionaryControllerPropertyActivator : IControllerPropertyActivator {
+    public class ViewDataDictionaryControllerPropertyActivator {
         public ViewDataDictionaryControllerPropertyActivator(IModelMetadataProvider modelMetadataProvider);
         public void Activate(ControllerContext actionContext, object controller);
         public Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor);
     }
     public static class ViewDataEvaluator {
         public static ViewDataInfo Eval(ViewDataDictionary viewData, string expression);
         public static ViewDataInfo Eval(object indexableObject, string expression);
     }
     public class ViewDataInfo {
         public ViewDataInfo(object container, object value);
         public ViewDataInfo(object container, PropertyInfo propertyInfo);
         public ViewDataInfo(object container, PropertyInfo propertyInfo, Func<object> valueAccessor);
         public object Container { get; }
         public PropertyInfo PropertyInfo { get; }
         public object Value { get; set; }
     }
     public class ViewExecutor {
         public static readonly string DefaultContentType;
+        protected ViewExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, DiagnosticListener diagnosticListener);
-        protected ViewExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, DiagnosticSource diagnosticSource);

+        public ViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, IModelMetadataProvider modelMetadataProvider);
-        public ViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, IModelMetadataProvider modelMetadataProvider);

-        protected DiagnosticSource DiagnosticSource { get; }
+        protected DiagnosticListener DiagnosticSource { get; }
         protected IModelMetadataProvider ModelMetadataProvider { get; }
         protected ITempDataDictionaryFactory TempDataFactory { get; }
         protected IViewEngine ViewEngine { get; }
         protected MvcViewOptions ViewOptions { get; }
         protected IHttpResponseStreamWriterFactory WriterFactory { get; }
         public virtual Task ExecuteAsync(ActionContext actionContext, IView view, ViewDataDictionary viewData, ITempDataDictionary tempData, string contentType, Nullable<int> statusCode);
         protected Task ExecuteAsync(ViewContext viewContext, string contentType, Nullable<int> statusCode);
     }
     public class ViewResultExecutor : ViewExecutor, IActionResultExecutor<ViewResult> {
+        public ViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);
-        public ViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);

         protected ILogger Logger { get; }
         public Task ExecuteAsync(ActionContext context, ViewResult result);
         public virtual ViewEngineResult FindView(ActionContext actionContext, ViewResult viewResult);
     }
 }
```

