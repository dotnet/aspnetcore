using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Default implementation of non-generic portions of <see cref="IHtmlHelper{T}">.
    /// </summary>
    public class HtmlHelper : ICanHasViewContext
    {
        public static readonly string ValidationInputCssClassName = "input-validation-error";
        public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
        public static readonly string ValidationMessageCssClassName = "field-validation-error";
        public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
        public static readonly string ValidationSummaryCssClassName = "validation-summary-errors";
        public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";

        private const string HiddenListItem = @"<li style=""display:none""></li>";

        private readonly IUrlHelper _urlHelper;
        private readonly IViewEngine _viewEngine;

        private ViewContext _viewContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlHelper"/> class.
        /// </summary>
        public HtmlHelper(
            [NotNull] IViewEngine viewEngine, 
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] IUrlHelper urlHelper)
        {
            _viewEngine = viewEngine;
            MetadataProvider = metadataProvider;
            _urlHelper = urlHelper;

            // Underscores are fine characters in id's.
            IdAttributeDotReplacement = "_";
        }

        public string IdAttributeDotReplacement { get; set; }

        public ViewContext ViewContext
        {
            get
            {
                if (_viewContext == null)
                {
                    throw new InvalidOperationException(Resources.HtmlHelper_NotContextualized);
                }

                return _viewContext;
            }
            private set
            {
                _viewContext = value;
            }
        }

        public dynamic ViewBag
        {
            get
            {
                return ViewContext.ViewBag;
            }
        }

        public ViewDataDictionary ViewData
        {
            get
            {
                return ViewContext.ViewData;
            }
        }

        protected IModelMetadataProvider MetadataProvider { get; private set; }

        public HtmlString ActionLink(
            [NotNull] string linkText, 
            string actionName, 
            string controllerName, 
            string protocol, 
            string hostname, 
            string fragment, 
            object routeValues, 
            object htmlAttributes)
        {
            var url = _urlHelper.Action(actionName, controllerName, routeValues);
            return GenerateLink(linkText, url, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// Creates a dictionary from an object, by adding each public instance property as a key with its associated 
        /// value to the dictionary. It will expose public properties from derived types as well. This is typically used
        /// with objects of an anonymous type.
        /// 
        /// If the object is already an <see cref="IDictionary{string, object}"/> instance, then it is
        /// returned as-is.
        /// </summary>
        /// <example>
        /// <c>new { property_name = "value" }</c> will translate to the entry <c>{ "property_name" , "value" }</c>
        /// in the resulting dictionary.
        /// </example>
        /// <param name="obj">The object to be converted.</param>
        /// <returns>The created dictionary of property names and property values.</returns>
        public static IDictionary<string, object> ObjectToDictionary(object obj)
        {
            return TypeHelper.ObjectToDictionary(obj);
        }

        /// <summary>
        /// Creates a dictionary of HTML attributes from the input object, 
        /// translating underscores to dashes in each public instance property.
        /// 
        /// If the object is already an <see cref="IDictionary{string, object}"/> instance, then it is
        /// returned as-is.
        /// <example>
        /// new { data_name="value" } will translate to the entry { "data-name" , "value" }
        /// in the resulting dictionary.
        /// </example>
        /// </summary>
        /// <param name="htmlAttributes">Anonymous object describing HTML attributes.</param>
        /// <returns>A dictionary that represents HTML attributes.</returns>
        public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes)
        {
            var dictionary = htmlAttributes as IDictionary<string, object>;
            if (dictionary != null)
            {
                return new Dictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase);
            }

            dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (htmlAttributes != null)
            {
                foreach (var helper in HtmlAttributePropertyHelper.GetProperties(htmlAttributes))
                {
                    dictionary.Add(helper.Name, helper.GetValue(htmlAttributes));
                }
            }

            return dictionary;
        }

        public virtual void Contextualize([NotNull] ViewContext viewContext)
        {
            ViewContext = viewContext;
        }

        public MvcForm BeginForm(string actionName, string controllerName, object routeValues, FormMethod method,
                                 object htmlAttributes)
        {
            // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            }

            return GenerateForm(actionName, controllerName, routeValues, method, htmlAttributeDictionary);
        }

        public void EndForm()
        {
            var mvcForm = CreateForm();
            mvcForm.EndForm();
        }

        public HtmlString CheckBox(string name, bool? isChecked, object htmlAttributes)
        {
            return GenerateCheckBox(metadata: null, name: name, isChecked: isChecked, htmlAttributes: htmlAttributes);
        }

        public string Encode(string value)
        {
            return (!string.IsNullOrEmpty(value)) ? WebUtility.HtmlEncode(value) : string.Empty;
        }

        public string Encode(object value)
        {
            return value != null ? WebUtility.HtmlEncode(value.ToString()) : string.Empty;
        }

        public string FormatValue(object value, string format)
        {
            return ViewDataDictionary.FormatValue(value, format);
        }

        public string GenerateIdFromName([NotNull] string name)
        {
            return TagBuilder.CreateSanitizedId(name, IdAttributeDotReplacement);
        }

        public HtmlString Display(string expression,
                                  string templateName,
                                  string htmlFieldName,
                                  object additionalViewData)
        {
            var metadata = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);

            return GenerateDisplay(metadata,
                                   htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                                   templateName,
                                   additionalViewData);
        }

        public HtmlString DisplayForModel(string templateName,
                                          string htmlFieldName,
                                          object additionalViewData)
        {
            return GenerateDisplay(ViewData.ModelMetadata,
                                   htmlFieldName,
                                   templateName,
                                   additionalViewData);
        }

        public HtmlString Hidden(string name, object value, object htmlAttributes)
        {
            return GenerateHidden(metadata: null, name: name, value: value, useViewData: (value == null),
                htmlAttributes: htmlAttributes);
        }

        public virtual HtmlString Name(string name)
        {
            var fullName = ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return new HtmlString(Encode(fullName));
        }

        public async Task<HtmlString> PartialAsync([NotNull] string partialViewName, object model,
                                                   ViewDataDictionary viewData)
        {
            using (var writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                await RenderPartialCoreAsync(partialViewName, model, viewData, writer);

                return new HtmlString(writer.ToString());
            }
        }

        public Task RenderPartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData)
        {
            return RenderPartialCoreAsync(partialViewName, model, viewData, ViewContext.Writer);
        }

        protected virtual HtmlString GenerateDisplay(ModelMetadata metadata,
                                                     string htmlFieldName,
                                                     string templateName,
                                                     object additionalViewData)
        {
            var templateBuilder = new TemplateBuilder(_viewEngine,
                                                      ViewContext,
                                                      ViewData,
                                                      metadata,
                                                      templateName,
                                                      templateName,
                                                      readOnly: true,
                                                      additionalViewData: additionalViewData);

            var templateResult = templateBuilder.Build();

            return new HtmlString(templateResult);
        }

        protected virtual async Task RenderPartialCoreAsync([NotNull] string partialViewName,
                                                            object model,
                                                            ViewDataDictionary viewData,
                                                            TextWriter writer)
        {
            // Determine which ViewData we should use to construct a new ViewData
            var baseViewData = viewData ?? ViewData;

            var newViewData = new ViewDataDictionary(baseViewData, model);

            var newViewContext = new ViewContext(ViewContext)
            {
                ViewData = newViewData,
                Writer = writer
            };

            var viewEngineResult = _viewEngine.FindPartialView(newViewContext.ViewEngineContext, partialViewName);

            await viewEngineResult.View.RenderAsync(newViewContext);
        }

        public HtmlString Password(string name, object value, object htmlAttributes)
        {
            return GeneratePassword(metadata: null, name: name, value: value, htmlAttributes: htmlAttributes);
        }

        public HtmlString RadioButton(string name, object value, bool? isChecked, object htmlAttributes)
        {
            return GenerateRadioButton(metadata: null, name: name, value: value, isChecked: isChecked,
                htmlAttributes: htmlAttributes);
        }

        public virtual HtmlString ValidationSummary(bool excludePropertyErrors, string message, IDictionary<string, object> htmlAttributes)
        {
            var formContext = ViewContext.ClientValidationEnabled ? ViewContext.FormContext : null;

            if (ViewData.ModelState.IsValid == true)
            {
                if (formContext == null ||
                    ViewContext.UnobtrusiveJavaScriptEnabled &&
                    excludePropertyErrors)
                {
                    // No client side validation/updates
                    return HtmlString.Empty;
                }
            }

            string messageSpan;
            if (!string.IsNullOrEmpty(message))
            {
                var spanTag = new TagBuilder("span");
                spanTag.SetInnerText(message);
                messageSpan = spanTag.ToString(TagRenderMode.Normal) + Environment.NewLine;
            }
            else
            {
                messageSpan = null;
            }

            var htmlSummary = new StringBuilder();
            var modelStates = ValidationHelpers.GetModelStateList(ViewData, excludePropertyErrors);

            foreach (var modelState in modelStates)
            {
                foreach (var modelError in modelState.Errors)
                {
                    string errorText = ValidationHelpers.GetUserErrorMessageOrDefault(modelError, modelState: null);

                    if (!string.IsNullOrEmpty(errorText))
                    {
                        var listItem = new TagBuilder("li");
                        listItem.SetInnerText(errorText);
                        htmlSummary.AppendLine(listItem.ToString(TagRenderMode.Normal));
                    }
                }
            }

            if (htmlSummary.Length == 0)
            {
                htmlSummary.AppendLine(HiddenListItem);
            }

            var unorderedList = new TagBuilder("ul")
            {
                InnerHtml = htmlSummary.ToString()
            };

            var divBuilder = new TagBuilder("div");
            divBuilder.MergeAttributes(htmlAttributes);

            if (ViewData.ModelState.IsValid == true)
            {
                divBuilder.AddCssClass(HtmlHelper.ValidationSummaryValidCssClassName);
            }
            else
            {
                divBuilder.AddCssClass(HtmlHelper.ValidationSummaryCssClassName);
            }

            divBuilder.InnerHtml = messageSpan + unorderedList.ToString(TagRenderMode.Normal);

            if (formContext != null)
            {
                if (ViewContext.UnobtrusiveJavaScriptEnabled)
                {
                    if (!excludePropertyErrors)
                    {
                        // Only put errors in the validation summary if they're supposed to be included there
                        divBuilder.MergeAttribute("data-valmsg-summary", "true");
                    }
                }
                else
                {
                    // client validation summaries need an ID
                    divBuilder.GenerateId("validationSummary", IdAttributeDotReplacement);
                    formContext.ValidationSummaryId = divBuilder.Attributes["id"];
                    formContext.ReplaceValidationSummary = !excludePropertyErrors;
                }
            }

            return divBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        /// <summary>
        /// Returns the HTTP method that handles form input (GET or POST) as a string.
        /// </summary>
        /// <param name="method">The HTTP method that handles the form.</param>
        /// <returns>The form method string, either "get" or "post".</returns>
        public static string GetFormMethodString(FormMethod method)
        {
            switch (method)
            {
                case FormMethod.Get:
                    return "get";
                case FormMethod.Post:
                    return "post";
                default:
                    return "post";
            }
        }

        public HtmlString TextBox(string name, object value, string format, IDictionary<string, object> htmlAttributes)
        {
            return GenerateTextBox(metadata: null, name: name, value: value, format: format,
                htmlAttributes: htmlAttributes);
        }

        public HtmlString Value([NotNull] string name, string format)
        {
            return GenerateValue(name, value: null, format: format, useViewData: true);
        }

        /// <summary>
        /// Override this method to return an <see cref="MvcForm"/> subclass. That subclass may change
        /// <see cref="EndForm()"/> behavior.
        /// </summary>
        /// <returns>A new <see cref="MvcForm"/> instance.</returns>
        protected virtual MvcForm CreateForm()
        {
            return new MvcForm(ViewContext);
        }

        protected bool EvalBoolean(string key)
        {
            return Convert.ToBoolean(ViewData.Eval(key), CultureInfo.InvariantCulture);
        }

        protected string EvalString(string key)
        {
            return Convert.ToString(ViewData.Eval(key), CultureInfo.CurrentCulture);
        }

        protected string EvalString(string key, string format)
        {
            return Convert.ToString(ViewData.Eval(key, format), CultureInfo.CurrentCulture);
        }

        protected object GetModelStateValue(string key, Type destinationType)
        {
            ModelState modelState;
            if (ViewData.ModelState.TryGetValue(key, out modelState) && modelState.Value != null)
            {
                return modelState.Value.ConvertTo(destinationType, culture: null);
            }

            return null;
        }

        protected IDictionary<string, object> GetValidationAttributes(string name)
        {
            return GetValidationAttributes(name, metadata: null);
        }

        // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
        // never rendered validation for a field with this name in this form. Also, if there's no form context,
        // then we can't render the attributes (we'd have no <form> to attach them to).
        protected IDictionary<string, object> GetValidationAttributes(string name, ModelMetadata metadata)
        {
            // TODO: Add validation attributes to input helpers.
            return new Dictionary<string, object>();
        }

        protected virtual HtmlString GenerateCheckBox(ModelMetadata metadata, string name, bool? isChecked,
            object htmlAttributes)
        {
            if (metadata != null)
            {
                // CheckBoxFor() case. That API does not support passing isChecked directly.
                Contract.Assert(!isChecked.HasValue);

                if (metadata.Model != null)
                {
                    bool modelChecked;
                    if (Boolean.TryParse(metadata.Model.ToString(), out modelChecked))
                    {
                        isChecked = modelChecked;
                    }
                }
            }

            // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributeDictionary == null)
                {
                    htmlAttributeDictionary = AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            var explicitValue = isChecked.HasValue;
            if (explicitValue && htmlAttributeDictionary != null)
            {
                // Explicit value must override dictionary
                htmlAttributeDictionary.Remove("checked");
            }

            return GenerateInput(InputType.CheckBox,
                metadata,
                name,
                value: "true",
                useViewData: !explicitValue,
                isChecked: isChecked ?? false,
                setId: true,
                isExplicitValue: false,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <summary>
        /// Writes an opening <form> tag to the response. When the user submits the form,
        /// the request will be processed by an action method.
        /// </summary>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">An object that contains the parameters for a route. The parameters are retrieved
        /// through reflection by examining the properties of the object. This object is typically created using object
        /// initializer syntax. Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the
        /// route parameters.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">An <see cref="IDictionary{string, object}"/> instance containing HTML
        /// attributes to set for the element.</param>
        /// <returns>An <see cref="MvcForm"/> instance which emits the closing {form} tag when disposed.</returns>
        protected virtual MvcForm GenerateForm(string actionName, string controllerName, object routeValues,
                                               FormMethod method, IDictionary<string, object> htmlAttributes)
        {
            var formAction = _urlHelper.Action(action: actionName, controller: controllerName, values: routeValues);

            var tagBuilder = new TagBuilder("form");
            tagBuilder.MergeAttributes(htmlAttributes);

            // action is implicitly generated, so htmlAttributes take precedence.
            tagBuilder.MergeAttribute("action", formAction);

            // method is an explicit parameter, so it takes precedence over the htmlAttributes.
            tagBuilder.MergeAttribute("method", HtmlHelper.GetFormMethodString(method), replaceExisting: true);

            var traditionalJavascriptEnabled = ViewContext.ClientValidationEnabled &&
                                               !ViewContext.UnobtrusiveJavaScriptEnabled;
            if (traditionalJavascriptEnabled)
            {
                // TODO revive ViewContext.FormIdGenerator(), WebFx-199
                // forms must have an ID for client validation
                var formName = "form" + new Guid().ToString();
                tagBuilder.GenerateId(formName, IdAttributeDotReplacement);
            }

            ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            var theForm = CreateForm();

            if (traditionalJavascriptEnabled)
            {
                ViewContext.FormContext.FormId = tagBuilder.Attributes["id"];
            }

            return theForm;
        }

        protected virtual HtmlString GenerateHidden(ModelMetadata metadata, string name, object value, bool useViewData,
            object htmlAttributes)
        {
            // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributeDictionary == null)
                {
                    htmlAttributeDictionary = AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            // Special-case opaque values and arbitrary binary data.
            var byteArrayValue = value as byte[];
            if (byteArrayValue != null)
            {
                value = Convert.ToBase64String(byteArrayValue);
            }

            return GenerateInput(InputType.Hidden,
                metadata,
                name,
                value,
                useViewData,
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        protected virtual HtmlString GenerateLink(
            [NotNull] string linkText,
            [NotNull] string url,
            IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("a")
            {
                InnerHtml = WebUtility.HtmlEncode(linkText),
            };

            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", url);

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual HtmlString GeneratePassword(ModelMetadata metadata, string name, object value,
            object htmlAttributes)
        {
            // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributeDictionary == null)
                {
                    htmlAttributeDictionary = AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            return GenerateInput(InputType.Password,
                metadata,
                name,
                value,
                useViewData: false,
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        protected virtual HtmlString GenerateRadioButton(ModelMetadata metadata, string name, object value,
            bool? isChecked, object htmlAttributes)
        {
            // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributeDictionary == null)
                {
                    htmlAttributeDictionary = AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            if (metadata == null)
            {
                // RadioButton() case. Do not override checked attribute if isChecked is implicit.
                if (!isChecked.HasValue &&
                    (htmlAttributeDictionary == null || !htmlAttributeDictionary.ContainsKey("checked")))
                {
                    // Note value may be null if isChecked is non-null.
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    // isChecked not provided nor found in the given attributes; fall back to view data.
                    var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
                    isChecked = !string.IsNullOrEmpty(name) &&
                        string.Equals(EvalString(name), valueString, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                // RadioButtonFor() case. That API does not support passing isChecked directly.
                Contract.Assert(!isChecked.HasValue);
                if (value == null)
                {
                    // Need a value to determine isChecked.
                    throw new ArgumentNullException("value");
                }

                var model = metadata.Model;
                var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
                isChecked = model != null &&
                    string.Equals(model.ToString(), valueString, StringComparison.OrdinalIgnoreCase);
            }

            var explicitValue = isChecked.HasValue;
            if (explicitValue && htmlAttributeDictionary != null)
            {
                // Explicit value must override dictionary
                htmlAttributeDictionary.Remove("checked");
            }

            return GenerateInput(InputType.Radio,
                metadata,
                name,
                value,
                useViewData: false,
                isChecked: isChecked ?? false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        protected virtual HtmlString GenerateTextBox(ModelMetadata metadata, string name, object value, string format,
            IDictionary<string, object> htmlAttributes)
        {
            return GenerateInput(InputType.Text,
                metadata,
                name,
                value,
                useViewData: (metadata == null && value == null),
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: format,
                htmlAttributes: htmlAttributes);
        }

        protected virtual HtmlString GenerateInput(InputType inputType, ModelMetadata metadata, string name,
            object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format,
            IDictionary<string, object> htmlAttributes)
        {
            // Not valid to use TextBoxForModel() and so on in a top-level view; would end up with an unnamed input
            // elements. But we support the *ForModel() methods in any lower-level template, once HtmlFieldPrefix is
            // non-empty.
            var fullName = ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "name");
            }

            var tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("type", GetInputTypeString(inputType));
            tagBuilder.MergeAttribute("name", fullName, replaceExisting: true);

            var valueParameter = FormatValue(value, format);
            var usedModelState = false;
            switch (inputType)
            {
                case InputType.CheckBox:
                    var modelStateWasChecked = GetModelStateValue(fullName, typeof(bool)) as bool?;
                    if (modelStateWasChecked.HasValue)
                    {
                        isChecked = modelStateWasChecked.Value;
                        usedModelState = true;
                    }

                    goto case InputType.Radio;

                case InputType.Radio:
                    if (!usedModelState)
                    {
                        var modelStateValue = GetModelStateValue(fullName, typeof(string)) as string;
                        if (modelStateValue != null)
                        {
                            isChecked = string.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                            usedModelState = true;
                        }
                    }

                    if (!usedModelState && useViewData)
                    {
                        isChecked = EvalBoolean(fullName);
                    }

                    if (isChecked)
                    {
                        tagBuilder.MergeAttribute("checked", "checked");
                    }

                    tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
                    break;

                case InputType.Password:
                    if (value != null)
                    {
                        tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
                    }

                    break;

                case InputType.Text:
                default:
                    var attributeValue = (string)GetModelStateValue(fullName, typeof(string));
                    if (attributeValue == null)
                    {
                        attributeValue = useViewData ? EvalString(fullName, format) : valueParameter;
                    }

                    tagBuilder.MergeAttribute("value", attributeValue, replaceExisting: isExplicitValue);
                    break;
            }

            if (setId)
            {
                tagBuilder.GenerateId(fullName, IdAttributeDotReplacement);
            }

            // If there are any errors for a named field, we add the CSS attribute.
            ModelState modelState;
            if (ViewData.ModelState.TryGetValue(fullName, out modelState) && modelState.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(ValidationInputCssClassName);
            }

            tagBuilder.MergeAttributes(GetValidationAttributes(name, metadata));

            if (inputType == InputType.CheckBox)
            {
                // Generate an additional <input type="hidden".../> for checkboxes. This
                // addresses scenarios where unchecked checkboxes are not sent in the request.
                // Sending a hidden input makes it possible to know that the checkbox was present
                // on the page when the request was submitted.
                var inputItemBuilder = new StringBuilder();
                inputItemBuilder.Append(tagBuilder.ToString(TagRenderMode.SelfClosing));

                var hiddenInput = new TagBuilder("input");
                hiddenInput.MergeAttribute("type", GetInputTypeString(InputType.Hidden));
                hiddenInput.MergeAttribute("name", fullName);
                hiddenInput.MergeAttribute("value", "false");
                inputItemBuilder.Append(hiddenInput.ToString(TagRenderMode.SelfClosing));
                return new HtmlString(inputItemBuilder.ToString());
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }


        protected virtual HtmlString GenerateValue(string name, object value, string format, bool useViewData)
        {
            var fullName = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            var attemptedValue = (string)GetModelStateValue(fullName, typeof(string));

            string resolvedValue;
            if (attemptedValue != null)
            {
                // case 1: if ModelState has a value then it's already formatted so ignore format string
                resolvedValue = attemptedValue;
            }
            else if (useViewData)
            {
                if (name.Length == 0)
                {
                    // case 2(a): format the value from ModelMetadata for the current model
                    var metadata = ViewData.ModelMetadata;
                    resolvedValue = FormatValue(metadata.Model, format);
                }
                else
                {
                    // case 2(b): format the value from ViewData
                    resolvedValue = EvalString(name, format);
                }
            }
            else
            {
                // case 3: format the explicit value from ModelMetadata
                resolvedValue = FormatValue(value, format);
            }

            return new HtmlString(Encode(resolvedValue));
        }

        private static string GetInputTypeString(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.CheckBox:
                    return "checkbox";
                case InputType.Hidden:
                    return "hidden";
                case InputType.Password:
                    return "password";
                case InputType.Radio:
                    return "radio";
                case InputType.Text:
                    return "text";
                default:
                    return "text";
            }
        }

        public HtmlString Raw(string value)
        {
            return new HtmlString(value);
        }

        public HtmlString Raw(object value)
        {
            return new HtmlString(value == null ? null : value.ToString());
        }
    }
}
