# Microsoft.AspNetCore.Mvc.Rendering

``` diff
 namespace Microsoft.AspNetCore.Mvc.Rendering {
     public enum FormMethod {
         Get = 0,
         Post = 1,
     }
     public enum Html5DateRenderingMode {
         CurrentCulture = 1,
         Rfc3339 = 0,
     }
+    public static class HtmlHelperComponentPrerenderingExtensions {
+        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent;
+        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper, object parameters) where TComponent : IComponent;
+    }
     public static class HtmlHelperDisplayExtensions {
         public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression, object additionalViewData);
         public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression, string templateName);
         public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression, string templateName, object additionalViewData);
         public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression, string templateName, string htmlFieldName);
         public static IHtmlContent DisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent DisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object additionalViewData);
         public static IHtmlContent DisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName);
         public static IHtmlContent DisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName, object additionalViewData);
         public static IHtmlContent DisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, object additionalViewData);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, string templateName);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, string templateName, object additionalViewData);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, string templateName, string htmlFieldName);
         public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, string templateName, string htmlFieldName, object additionalViewData);
     }
     public static class HtmlHelperDisplayNameExtensions {
         public static string DisplayNameFor<TModelItem, TResult>(this IHtmlHelper<IEnumerable<TModelItem>> htmlHelper, Expression<Func<TModelItem, TResult>> expression);
         public static string DisplayNameForModel(this IHtmlHelper htmlHelper);
     }
     public static class HtmlHelperEditorExtensions {
         public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression, object additionalViewData);
         public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression, string templateName);
         public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression, string templateName, object additionalViewData);
         public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression, string templateName, string htmlFieldName);
         public static IHtmlContent EditorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent EditorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object additionalViewData);
         public static IHtmlContent EditorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName);
         public static IHtmlContent EditorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName, object additionalViewData);
         public static IHtmlContent EditorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, object additionalViewData);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, string templateName);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, string templateName, object additionalViewData);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, string templateName, string htmlFieldName);
         public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, string templateName, string htmlFieldName, object additionalViewData);
     }
     public static class HtmlHelperFormExtensions {
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, FormMethod method);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, FormMethod method, object htmlAttributes);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, Nullable<bool> antiforgery);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, object routeValues);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, string actionName, string controllerName);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, string actionName, string controllerName, FormMethod method);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, string actionName, string controllerName, FormMethod method, object htmlAttributes);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, string actionName, string controllerName, object routeValues);
         public static MvcForm BeginForm(this IHtmlHelper htmlHelper, string actionName, string controllerName, object routeValues, FormMethod method);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, object routeValues);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, object routeValues, Nullable<bool> antiforgery);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, FormMethod method);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, FormMethod method, object htmlAttributes);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, Nullable<bool> antiforgery);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, object routeValues);
         public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, object routeValues, FormMethod method);
     }
     public static class HtmlHelperInputExtensions {
         public static IHtmlContent CheckBox(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent CheckBox(this IHtmlHelper htmlHelper, string expression, bool isChecked);
         public static IHtmlContent CheckBox(this IHtmlHelper htmlHelper, string expression, object htmlAttributes);
         public static IHtmlContent CheckBoxFor<TModel>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression);
         public static IHtmlContent Hidden(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent Hidden(this IHtmlHelper htmlHelper, string expression, object value);
         public static IHtmlContent HiddenFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent Password(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent Password(this IHtmlHelper htmlHelper, string expression, object value);
         public static IHtmlContent PasswordFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent RadioButton(this IHtmlHelper htmlHelper, string expression, object value);
         public static IHtmlContent RadioButton(this IHtmlHelper htmlHelper, string expression, object value, bool isChecked);
         public static IHtmlContent RadioButton(this IHtmlHelper htmlHelper, string expression, object value, object htmlAttributes);
         public static IHtmlContent RadioButtonFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object value);
         public static IHtmlContent TextArea(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent TextArea(this IHtmlHelper htmlHelper, string expression, object htmlAttributes);
         public static IHtmlContent TextArea(this IHtmlHelper htmlHelper, string expression, string value);
         public static IHtmlContent TextArea(this IHtmlHelper htmlHelper, string expression, string value, object htmlAttributes);
         public static IHtmlContent TextAreaFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent TextAreaFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         public static IHtmlContent TextBox(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent TextBox(this IHtmlHelper htmlHelper, string expression, object value);
         public static IHtmlContent TextBox(this IHtmlHelper htmlHelper, string expression, object value, object htmlAttributes);
         public static IHtmlContent TextBox(this IHtmlHelper htmlHelper, string expression, object value, string format);
         public static IHtmlContent TextBoxFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent TextBoxFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         public static IHtmlContent TextBoxFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string format);
     }
     public static class HtmlHelperLabelExtensions {
         public static IHtmlContent Label(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent Label(this IHtmlHelper htmlHelper, string expression, string labelText);
         public static IHtmlContent LabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent LabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         public static IHtmlContent LabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string labelText);
         public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper);
         public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper, object htmlAttributes);
         public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper, string labelText);
         public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper, string labelText, object htmlAttributes);
     }
     public static class HtmlHelperLinkExtensions {
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName);
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName, object routeValues);
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName, object routeValues, object htmlAttributes);
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName, string controllerName);
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName, string controllerName, object routeValues);
         public static IHtmlContent ActionLink(this IHtmlHelper helper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes);
         public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, string linkText, object routeValues);
         public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, string linkText, object routeValues, object htmlAttributes);
         public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, string linkText, string routeName);
         public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, string linkText, string routeName, object routeValues);
         public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, string linkText, string routeName, object routeValues, object htmlAttributes);
     }
     public static class HtmlHelperNameExtensions {
         public static string IdForModel(this IHtmlHelper htmlHelper);
         public static string NameForModel(this IHtmlHelper htmlHelper);
     }
     public static class HtmlHelperPartialExtensions {
         public static IHtmlContent Partial(this IHtmlHelper htmlHelper, string partialViewName);
         public static IHtmlContent Partial(this IHtmlHelper htmlHelper, string partialViewName, ViewDataDictionary viewData);
         public static IHtmlContent Partial(this IHtmlHelper htmlHelper, string partialViewName, object model);
         public static IHtmlContent Partial(this IHtmlHelper htmlHelper, string partialViewName, object model, ViewDataDictionary viewData);
         public static Task<IHtmlContent> PartialAsync(this IHtmlHelper htmlHelper, string partialViewName);
         public static Task<IHtmlContent> PartialAsync(this IHtmlHelper htmlHelper, string partialViewName, ViewDataDictionary viewData);
         public static Task<IHtmlContent> PartialAsync(this IHtmlHelper htmlHelper, string partialViewName, object model);
         public static void RenderPartial(this IHtmlHelper htmlHelper, string partialViewName);
         public static void RenderPartial(this IHtmlHelper htmlHelper, string partialViewName, ViewDataDictionary viewData);
         public static void RenderPartial(this IHtmlHelper htmlHelper, string partialViewName, object model);
         public static void RenderPartial(this IHtmlHelper htmlHelper, string partialViewName, object model, ViewDataDictionary viewData);
         public static Task RenderPartialAsync(this IHtmlHelper htmlHelper, string partialViewName);
         public static Task RenderPartialAsync(this IHtmlHelper htmlHelper, string partialViewName, ViewDataDictionary viewData);
         public static Task RenderPartialAsync(this IHtmlHelper htmlHelper, string partialViewName, object model);
     }
+    public static class HtmlHelperRazorComponentExtensions {
+        public static Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent;
+        public static Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(this IHtmlHelper htmlHelper, object parameters) where TComponent : IComponent;
+    }
     public static class HtmlHelperSelectExtensions {
         public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression, IEnumerable<SelectListItem> selectList);
         public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression, IEnumerable<SelectListItem> selectList, string optionLabel);
         public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression, string optionLabel);
         public static IHtmlContent DropDownListFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList);
         public static IHtmlContent DropDownListFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         public static IHtmlContent DropDownListFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, string optionLabel);
         public static IHtmlContent ListBox(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent ListBox(this IHtmlHelper htmlHelper, string expression, IEnumerable<SelectListItem> selectList);
         public static IHtmlContent ListBoxFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList);
     }
     public static class HtmlHelperValidationExtensions {
         public static IHtmlContent ValidationMessage(this IHtmlHelper htmlHelper, string expression);
         public static IHtmlContent ValidationMessage(this IHtmlHelper htmlHelper, string expression, object htmlAttributes);
         public static IHtmlContent ValidationMessage(this IHtmlHelper htmlHelper, string expression, string message);
         public static IHtmlContent ValidationMessage(this IHtmlHelper htmlHelper, string expression, string message, object htmlAttributes);
         public static IHtmlContent ValidationMessage(this IHtmlHelper htmlHelper, string expression, string message, string tag);
         public static IHtmlContent ValidationMessageFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static IHtmlContent ValidationMessageFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string message);
         public static IHtmlContent ValidationMessageFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string message, object htmlAttributes);
         public static IHtmlContent ValidationMessageFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string message, string tag);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, bool excludePropertyErrors);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, bool excludePropertyErrors, string message);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, bool excludePropertyErrors, string message, object htmlAttributes);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, bool excludePropertyErrors, string message, string tag);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message, object htmlAttributes);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message, object htmlAttributes, string tag);
         public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message, string tag);
     }
     public static class HtmlHelperValueExtensions {
         public static string Value(this IHtmlHelper htmlHelper, string expression);
         public static string ValueFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression);
         public static string ValueForModel(this IHtmlHelper htmlHelper);
         public static string ValueForModel(this IHtmlHelper htmlHelper, string format);
     }
     public interface IHtmlHelper {
         Html5DateRenderingMode Html5DateRenderingMode { get; set; }
         string IdAttributeDotReplacement { get; }
         IModelMetadataProvider MetadataProvider { get; }
         ITempDataDictionary TempData { get; }
         UrlEncoder UrlEncoder { get; }
         dynamic ViewBag { get; }
         ViewContext ViewContext { get; }
         ViewDataDictionary ViewData { get; }
         IHtmlContent ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes);
         IHtmlContent AntiForgeryToken();
         MvcForm BeginForm(string actionName, string controllerName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         MvcForm BeginRouteForm(string routeName, object routeValues, FormMethod method, Nullable<bool> antiforgery, object htmlAttributes);
         IHtmlContent CheckBox(string expression, Nullable<bool> isChecked, object htmlAttributes);
         IHtmlContent Display(string expression, string templateName, string htmlFieldName, object additionalViewData);
         string DisplayName(string expression);
         string DisplayText(string expression);
         IHtmlContent DropDownList(string expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes);
         IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);
         string Encode(object value);
         string Encode(string value);
         void EndForm();
         string FormatValue(object value, string format);
         string GenerateIdFromName(string fullName);
         IEnumerable<SelectListItem> GetEnumSelectList(Type enumType);
-        IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct, ValueType;
+        IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;
         IHtmlContent Hidden(string expression, object value, object htmlAttributes);
         string Id(string expression);
         IHtmlContent Label(string expression, string labelText, object htmlAttributes);
         IHtmlContent ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         string Name(string expression);
         Task<IHtmlContent> PartialAsync(string partialViewName, object model, ViewDataDictionary viewData);
         IHtmlContent Password(string expression, object value, object htmlAttributes);
         IHtmlContent RadioButton(string expression, object value, Nullable<bool> isChecked, object htmlAttributes);
         IHtmlContent Raw(object value);
         IHtmlContent Raw(string value);
         Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData);
         IHtmlContent RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes);
         IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes);
         IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes);
         IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag);
         IHtmlContent ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag);
         string Value(string expression, string format);
     }
     public interface IHtmlHelper<TModel> : IHtmlHelper {
         new ViewDataDictionary<TModel> ViewData { get; }
         IHtmlContent CheckBoxFor(Expression<Func<TModel, bool>> expression, object htmlAttributes);
         IHtmlContent DisplayFor<TResult>(Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
         string DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression);
         string DisplayNameForInnerType<TModelItem, TResult>(Expression<Func<TModelItem, TResult>> expression);
         string DisplayTextFor<TResult>(Expression<Func<TModel, TResult>> expression);
         IHtmlContent DropDownListFor<TResult>(Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes);
         IHtmlContent EditorFor<TResult>(Expression<Func<TModel, TResult>> expression, string templateName, string htmlFieldName, object additionalViewData);
         new string Encode(object value);
         new string Encode(string value);
         IHtmlContent HiddenFor<TResult>(Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         string IdFor<TResult>(Expression<Func<TModel, TResult>> expression);
         IHtmlContent LabelFor<TResult>(Expression<Func<TModel, TResult>> expression, string labelText, object htmlAttributes);
         IHtmlContent ListBoxFor<TResult>(Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);
         string NameFor<TResult>(Expression<Func<TModel, TResult>> expression);
         IHtmlContent PasswordFor<TResult>(Expression<Func<TModel, TResult>> expression, object htmlAttributes);
         IHtmlContent RadioButtonFor<TResult>(Expression<Func<TModel, TResult>> expression, object value, object htmlAttributes);
         new IHtmlContent Raw(object value);
         new IHtmlContent Raw(string value);
         IHtmlContent TextAreaFor<TResult>(Expression<Func<TModel, TResult>> expression, int rows, int columns, object htmlAttributes);
         IHtmlContent TextBoxFor<TResult>(Expression<Func<TModel, TResult>> expression, string format, object htmlAttributes);
         IHtmlContent ValidationMessageFor<TResult>(Expression<Func<TModel, TResult>> expression, string message, object htmlAttributes, string tag);
         string ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string format);
     }
     public interface IJsonHelper {
         IHtmlContent Serialize(object value);
-        IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings);

     }
     public class MultiSelectList : IEnumerable, IEnumerable<SelectListItem> {
         public MultiSelectList(IEnumerable items);
         public MultiSelectList(IEnumerable items, IEnumerable selectedValues);
         public MultiSelectList(IEnumerable items, string dataValueField, string dataTextField);
         public MultiSelectList(IEnumerable items, string dataValueField, string dataTextField, IEnumerable selectedValues);
         public MultiSelectList(IEnumerable items, string dataValueField, string dataTextField, IEnumerable selectedValues, string dataGroupField);
         public string DataGroupField { get; }
         public string DataTextField { get; }
         public string DataValueField { get; }
         public IEnumerable Items { get; }
         public IEnumerable SelectedValues { get; }
         public virtual IEnumerator<SelectListItem> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class MvcForm : IDisposable {
         public MvcForm(ViewContext viewContext, HtmlEncoder htmlEncoder);
         public void Dispose();
         public void EndForm();
         protected virtual void GenerateEndForm();
     }
     public class SelectList : MultiSelectList {
         public SelectList(IEnumerable items);
         public SelectList(IEnumerable items, object selectedValue);
         public SelectList(IEnumerable items, string dataValueField, string dataTextField);
         public SelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue);
         public SelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue, string dataGroupField);
         public object SelectedValue { get; }
     }
     public class SelectListGroup {
         public SelectListGroup();
         public bool Disabled { get; set; }
         public string Name { get; set; }
     }
     public class SelectListItem {
         public SelectListItem();
         public SelectListItem(string text, string value);
         public SelectListItem(string text, string value, bool selected);
         public SelectListItem(string text, string value, bool selected, bool disabled);
         public bool Disabled { get; set; }
         public SelectListGroup Group { get; set; }
         public bool Selected { get; set; }
         public string Text { get; set; }
         public string Value { get; set; }
     }
     public class TagBuilder : IHtmlContent {
         public TagBuilder(string tagName);
         public AttributeDictionary Attributes { get; }
         public bool HasInnerHtml { get; }
         public IHtmlContentBuilder InnerHtml { get; }
         public string TagName { get; }
         public TagRenderMode TagRenderMode { get; set; }
         public void AddCssClass(string value);
         public static string CreateSanitizedId(string name, string invalidCharReplacement);
         public void GenerateId(string name, string invalidCharReplacement);
         public void MergeAttribute(string key, string value);
         public void MergeAttribute(string key, string value, bool replaceExisting);
         public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes);
         public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting);
         public IHtmlContent RenderBody();
         public IHtmlContent RenderEndTag();
         public IHtmlContent RenderSelfClosingTag();
         public IHtmlContent RenderStartTag();
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public enum TagRenderMode {
         EndTag = 2,
         Normal = 0,
         SelfClosing = 3,
         StartTag = 1,
     }
     public enum ValidationSummary {
         All = 2,
         ModelOnly = 1,
         None = 0,
     }
     public static class ViewComponentHelperExtensions {
         public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper helper, string name);
         public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper helper, Type componentType);
         public static Task<IHtmlContent> InvokeAsync<TComponent>(this IViewComponentHelper helper);
         public static Task<IHtmlContent> InvokeAsync<TComponent>(this IViewComponentHelper helper, object arguments);
     }
     public class ViewContext : ActionContext {
         public ViewContext();
         public ViewContext(ActionContext actionContext, IView view, ViewDataDictionary viewData, ITempDataDictionary tempData, TextWriter writer, HtmlHelperOptions htmlHelperOptions);
         public ViewContext(ViewContext viewContext, IView view, ViewDataDictionary viewData, TextWriter writer);
         public bool ClientValidationEnabled { get; set; }
         public string ExecutingFilePath { get; set; }
         public virtual FormContext FormContext { get; set; }
         public Html5DateRenderingMode Html5DateRenderingMode { get; set; }
         public ITempDataDictionary TempData { get; set; }
         public string ValidationMessageElement { get; set; }
         public string ValidationSummaryMessageElement { get; set; }
         public IView View { get; set; }
         public dynamic ViewBag { get; }
         public ViewDataDictionary ViewData { get; set; }
         public TextWriter Writer { get; set; }
         public FormContext GetFormContextForClientValidation();
     }
 }
```

