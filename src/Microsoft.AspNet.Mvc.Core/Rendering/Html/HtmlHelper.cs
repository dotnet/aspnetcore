// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Rendering.Expressions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Default implementation of <see cref="IHtmlHelper"/>.
    /// </summary>
    public class HtmlHelper : IHtmlHelper, ICanHasViewContext
    {
        public static readonly string ValidationInputCssClassName = "input-validation-error";
        public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
        public static readonly string ValidationMessageCssClassName = "field-validation-error";
        public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
        public static readonly string ValidationSummaryCssClassName = "validation-summary-errors";
        public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";

        private readonly IHtmlGenerator _htmlGenerator;
        private readonly ICompositeViewEngine _viewEngine;

        private ViewContext _viewContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlHelper"/> class.
        /// </summary>
        public HtmlHelper(
            [NotNull] IHtmlGenerator htmlGenerator,
            [NotNull] ICompositeViewEngine viewEngine,
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] IHtmlEncoder htmlEncoder,
            [NotNull] IUrlEncoder urlEncoder,
            [NotNull] IJavaScriptStringEncoder javaScriptStringEncoder)
        {
            _viewEngine = viewEngine;
            _htmlGenerator = htmlGenerator;
            MetadataProvider = metadataProvider;
            HtmlEncoder = htmlEncoder;
            UrlEncoder = urlEncoder;
            JavaScriptStringEncoder = javaScriptStringEncoder;
        }

        /// <inheritdoc />
        public Html5DateRenderingMode Html5DateRenderingMode
        {
            get
            {
                return ViewContext.Html5DateRenderingMode;
            }
            set
            {
                ViewContext.Html5DateRenderingMode = value;
            }
        }

        /// <inheritdoc />
        public string IdAttributeDotReplacement
        {
            get
            {
                return _htmlGenerator.IdAttributeDotReplacement;
            }
            set
            {
                _htmlGenerator.IdAttributeDotReplacement = value;
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public dynamic ViewBag
        {
            get
            {
                return ViewContext.ViewBag;
            }
        }

        /// <inheritdoc />
        public ViewDataDictionary ViewData
        {
            get
            {
                return ViewContext.ViewData;
            }
        }

        /// <inheritdoc />
        public ITempDataDictionary TempData
        {
            get
            {
                return ViewContext.TempData;
            }
        }

        /// <inheritdoc />
        public IHtmlEncoder HtmlEncoder { get; }

        /// <inheritdoc />
        public IUrlEncoder UrlEncoder { get; }

        /// <inheritdoc />
        public IJavaScriptStringEncoder JavaScriptStringEncoder { get; }

        /// <inheritdoc />
        public IModelMetadataProvider MetadataProvider { get; }

        /// <summary>
        /// Creates a dictionary from an object, by adding each public instance property as a key with its associated
        /// value to the dictionary. It will expose public properties from derived types as well. This is typically
        /// used with objects of an anonymous type.
        ///
        /// If the <paramref name="value"/> is already an <see cref="IDictionary{string, object}"/> instance, then it
        /// is returned as-is.
        /// <example>
        /// <c>new { data_name="value" }</c> will translate to the entry <c>{ "data_name", "value" }</c>
        /// in the resulting dictionary.
        /// </example>
        /// </summary>
        /// <param name="value">The <see cref="object"/> to be converted.</param>
        /// <returns>The created dictionary of property names and property values.</returns>
        public static IDictionary<string, object> ObjectToDictionary(object value)
        {
            return TypeHelper.ObjectToDictionary(value);
        }

        /// <summary>
        /// Creates a dictionary of HTML attributes from the input object,
        /// translating underscores to dashes in each public instance property.
        ///
        /// If the object is already an <see cref="IDictionary{string, object}"/> instance, then it is
        /// returned as-is.
        /// <example>
        /// <c>new { data_name="value" }</c> will translate to the entry <c>{ "data-name", "value" }</c>
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
                    dictionary[helper.Name] = helper.GetValue(htmlAttributes);
                }
            }

            return dictionary;
        }

        public virtual void Contextualize([NotNull] ViewContext viewContext)
        {
            ViewContext = viewContext;
        }

        /// <inheritdoc />
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
            var tagBuilder = _htmlGenerator.GenerateActionLink(
                linkText,
                actionName,
                controllerName,
                protocol,
                hostname,
                fragment,
                routeValues,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        /// <inheritdoc />
        public HtmlString AntiForgeryToken()
        {
            var tagBuilder = _htmlGenerator.GenerateAntiForgery(ViewContext);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }

        /// <inheritdoc />
        public MvcForm BeginForm(
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            return GenerateForm(actionName, controllerName, routeValues, method, htmlAttributes);
        }

        /// <inheritdoc />
        public MvcForm BeginRouteForm(string routeName, object routeValues, FormMethod method, object htmlAttributes)
        {
            return GenerateRouteForm(routeName, routeValues, method, htmlAttributes);
        }

        /// <inheritdoc />
        public void EndForm()
        {
            var mvcForm = CreateForm();
            mvcForm.EndForm();
        }

        /// <inheritdoc />
        public HtmlString CheckBox(string expression, bool? isChecked, object htmlAttributes)
        {
            return GenerateCheckBox(
                modelExplorer: null,
                expression: expression,
                isChecked: isChecked,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string Encode(string value)
        {
            return _htmlGenerator.Encode(value);
        }

        /// <inheritdoc />
        public string Encode(object value)
        {
            return _htmlGenerator.Encode(value);
        }

        /// <inheritdoc />
        public string FormatValue(object value, string format)
        {
            return _htmlGenerator.FormatValue(value, format);
        }

        /// <inheritdoc />
        public string GenerateIdFromName([NotNull] string fullName)
        {
            return TagBuilder.CreateSanitizedId(fullName, IdAttributeDotReplacement);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public string DisplayName(string expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);
            return GenerateDisplayName(modelExplorer, expression);
        }

        /// <inheritdoc />
        public string DisplayText(string expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);
            return GenerateDisplayText(modelExplorer);
        }

        /// <inheritdoc />
        public HtmlString DropDownList(
            string expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes)
        {
            return GenerateDropDown(
                modelExplorer: null,
                expression: expression,
                selectList: selectList,
                optionLabel: optionLabel,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString Editor(
            string expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);

            return GenerateEditor(
                modelExplorer,
                htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                templateName,
                additionalViewData);
        }

        /// <inheritdoc />
        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct
        {
            var type = typeof(TEnum);
            var metadata = MetadataProvider.GetMetadataForType(type);
            if (!metadata.IsEnum || metadata.IsFlagsEnum)
            {
                var message = Resources.FormatHtmlHelper_TypeNotSupported_ForGetEnumSelectList(
                    type.FullName,
                    nameof(Enum).ToLowerInvariant(),
                    nameof(FlagsAttribute));
                throw new ArgumentException(message, nameof(TEnum));
            }

            return GetEnumSelectList(metadata);
        }

        /// <inheritdoc />
        public IEnumerable<SelectListItem> GetEnumSelectList([NotNull] Type enumType)
        {
            var metadata = MetadataProvider.GetMetadataForType(enumType);
            if (!metadata.IsEnum || metadata.IsFlagsEnum)
            {
                var message = Resources.FormatHtmlHelper_TypeNotSupported_ForGetEnumSelectList(
                    enumType.FullName,
                    nameof(Enum).ToLowerInvariant(),
                    nameof(FlagsAttribute));
                throw new ArgumentException(message, nameof(enumType));
            }

            return GetEnumSelectList(metadata);
        }

        /// <inheritdoc />
        public HtmlString Hidden(string expression, object value, object htmlAttributes)
        {
            return GenerateHidden(
                modelExplorer: null,
                expression: expression,
                value: value,
                useViewData: (value == null),
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string Id(string expression)
        {
            return GenerateId(expression);
        }

        /// <inheritdoc />
        public HtmlString Label(string expression, string labelText, object htmlAttributes)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);
            return GenerateLabel(
                modelExplorer,
                expression,
                labelText,
                htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return GenerateListBox(
                modelExplorer: null,
                expression: expression,
                selectList: selectList,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string Name(string expression)
        {
            return GenerateName(expression);
        }

        /// <inheritdoc />
        public async Task<HtmlString> PartialAsync(
            [NotNull] string partialViewName,
            object model,
            ViewDataDictionary viewData)
        {
            using (var writer = new StringCollectionTextWriter(Encoding.UTF8))
            {
                await RenderPartialCoreAsync(partialViewName, model, viewData, writer);

                return new HtmlString(writer);
            }
        }

        /// <inheritdoc />
        public Task RenderPartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData)
        {
            return RenderPartialCoreAsync(partialViewName, model, viewData, ViewContext.Writer);
        }

        protected virtual HtmlString GenerateDisplay(ModelExplorer modelExplorer,
                                                     string htmlFieldName,
                                                     string templateName,
                                                     object additionalViewData)
        {
            var templateBuilder = new TemplateBuilder(_viewEngine,
                                                      ViewContext,
                                                      ViewData,
                                                      modelExplorer,
                                                      htmlFieldName,
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

            var viewEngineResult = _viewEngine.FindPartialView(ViewContext, partialViewName);
            if (!viewEngineResult.Success)
            {
                var locations = string.Empty;
                if (viewEngineResult.SearchedLocations != null)
                {
                    locations = Environment.NewLine +
                        string.Join(Environment.NewLine, viewEngineResult.SearchedLocations);
                }

                throw new InvalidOperationException(
                    Resources.FormatViewEngine_PartialViewNotFound(partialViewName, locations));
            }

            var view = viewEngineResult.View;
            using (view as IDisposable)
            {
                var viewContext = new ViewContext(ViewContext, view, newViewData, TempData, writer);
                await viewEngineResult.View.RenderAsync(viewContext);
            }
        }

        /// <inheritdoc />
        public HtmlString Password(string expression, object value, object htmlAttributes)
        {
            return GeneratePassword(
                modelExplorer: null,
                expression: expression,
                value: value,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString RadioButton(string expression, object value, bool? isChecked, object htmlAttributes)
        {
            return GenerateRadioButton(
                modelExplorer: null,
                expression: expression,
                value: value,
                isChecked: isChecked,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString Raw(string value)
        {
            return new HtmlString(value);
        }

        /// <inheritdoc />
        public HtmlString Raw(object value)
        {
            return new HtmlString(value == null ? null : value.ToString());
        }

        /// <inheritdoc />
        public HtmlString RouteLink(
            [NotNull] string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateRouteLink(
                linkText,
                routeName,
                protocol,
                hostName,
                fragment,
                routeValues,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        /// <inheritdoc />
        public HtmlString ValidationMessage(string expression, string message, object htmlAttributes, string tag)
        {
            return GenerateValidationMessage(expression, message, htmlAttributes, tag);
        }

        /// <inheritdoc />
        public HtmlString ValidationSummary(
            bool excludePropertyErrors,
            string message,
            object htmlAttributes,
            string tag)
        {
            return GenerateValidationSummary(excludePropertyErrors, message, htmlAttributes, tag);
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

        /// <inheritdoc />
        public HtmlString TextArea(string expression, string value, int rows, int columns, object htmlAttributes)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, ViewData, MetadataProvider);
            if (value != null)
            {
                // As a special case we allow treating a string value as a model of arbitrary type.
                // So pass through the string representation, even though the ModelMetadata might
                // be for some other type.
                //
                // We do this because thought we're displaying something as a string, we want to have
                // the right set of validation attributes.
                modelExplorer = new ModelExplorer(
                    MetadataProvider,
                    modelExplorer.Container,
                    modelExplorer.Metadata,
                    value);
            }

            return GenerateTextArea(modelExplorer, expression, rows, columns, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString TextBox(string expression, object value, string format, object htmlAttributes)
        {
            return GenerateTextBox(modelExplorer: null, expression: expression, value: value, format: format,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string Value(string expression, string format)
        {
            return GenerateValue(expression, value: null, format: format, useViewData: true);
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

        protected virtual HtmlString GenerateCheckBox(
            ModelExplorer modelExplorer,
            string expression,
            bool? isChecked,
            object htmlAttributes)
        {
            var checkbox = _htmlGenerator.GenerateCheckBox(
                ViewContext,
                modelExplorer,
                expression,
                isChecked,
                htmlAttributes);

            var hidden = _htmlGenerator.GenerateHiddenForCheckbox(ViewContext, modelExplorer, expression);
            if (checkbox == null || hidden == null)
            {
                return HtmlString.Empty;
            }

            var elements = checkbox.ToString(TagRenderMode.SelfClosing) + hidden.ToString(TagRenderMode.SelfClosing);

            return new HtmlString(elements);
        }

        protected virtual string GenerateDisplayName([NotNull] ModelExplorer modelExplorer, string expression)
        {
            // We don't call ModelMetadata.GetDisplayName here because
            // we want to fall back to the field name rather than the ModelType.
            // This is similar to how the GenerateLabel get the text of a label.
            var resolvedDisplayName = modelExplorer.Metadata.DisplayName ?? modelExplorer.Metadata.PropertyName;
            if (resolvedDisplayName == null)
            {
                resolvedDisplayName =
                    string.IsNullOrEmpty(expression) ? string.Empty : expression.Split('.').Last();
            }

            return resolvedDisplayName;
        }

        protected virtual string GenerateDisplayText(ModelExplorer modelExplorer)
        {
            return modelExplorer.GetSimpleDisplayText() ?? string.Empty;
        }

        protected HtmlString GenerateDropDown(
            ModelExplorer modelExplorer,
            string expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateSelect(
                ViewContext,
                modelExplorer,
                optionLabel,
                expression,
                selectList,
                allowMultiple: false,
                htmlAttributes: htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual HtmlString GenerateEditor(
            ModelExplorer modelExplorer,
            string htmlFieldName,
            string templateName,
            object additionalViewData)
        {
            var templateBuilder = new TemplateBuilder(
                _viewEngine,
                ViewContext,
                ViewData,
                modelExplorer,
                htmlFieldName,
                templateName,
                readOnly: false,
                additionalViewData: additionalViewData);

            var templateResult = templateBuilder.Build();

            return new HtmlString(templateResult);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        protected virtual MvcForm GenerateForm(
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateForm(
                ViewContext,
                actionName,
                controllerName,
                routeValues,
                GetFormMethodString(method),
                htmlAttributes);
            if (tagBuilder != null)
            {
                ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            }

            return CreateForm();
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        protected virtual MvcForm GenerateRouteForm(
            string routeName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateRouteForm(
                ViewContext,
                routeName,
                routeValues,
                GetFormMethodString(method),
                htmlAttributes);
            if (tagBuilder != null)
            {
                ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            }

            return CreateForm();
        }

        protected virtual HtmlString GenerateHidden(
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool useViewData,
            object htmlAttributes)
        {
            var tagBuilder =
                _htmlGenerator.GenerateHidden(
                    ViewContext,
                    modelExplorer,
                    expression,
                    value,
                    useViewData,
                    htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }

        protected virtual string GenerateId(string expression)
        {
            var fullName = DefaultHtmlGenerator.GetFullHtmlFieldName(ViewContext, expression: expression);
            var id = TagBuilder.CreateSanitizedId(fullName, IdAttributeDotReplacement);

            return id;
        }

        protected virtual HtmlString GenerateLabel(
            [NotNull] ModelExplorer modelExplorer,
            string expression,
            string labelText,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateLabel(
                ViewContext,
                modelExplorer,
                expression: expression,
                labelText: labelText,
                htmlAttributes: htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected HtmlString GenerateListBox(
            ModelExplorer modelExplorer,
            string expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateSelect(
                ViewContext,
                modelExplorer,
                optionLabel: null,
                expression: expression,
                selectList: selectList,
                allowMultiple: true,
                htmlAttributes: htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual string GenerateName(string expression)
        {
            var fullName = DefaultHtmlGenerator.GetFullHtmlFieldName(ViewContext, expression);
            return fullName;
        }

        protected virtual HtmlString GeneratePassword(
            ModelExplorer modelExplorer,
            string expression,
            object value,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GeneratePassword(
                ViewContext,
                modelExplorer,
                expression,
                value,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }

        protected virtual HtmlString GenerateRadioButton(
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool? isChecked,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateRadioButton(
                ViewContext,
                modelExplorer,
                expression,
                value,
                isChecked,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }

        protected virtual HtmlString GenerateTextArea(
            ModelExplorer modelExplorer,
            string expression,
            int rows,
            int columns,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateTextArea(
                ViewContext,
                modelExplorer,
                expression,
                rows,
                columns,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual HtmlString GenerateTextBox(
            ModelExplorer modelExplorer,
            string expression,
            object value,
            string format,
            object htmlAttributes)
        {
            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                modelExplorer,
                expression,
                value,
                format,
                htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.SelfClosing);
        }

        protected virtual HtmlString GenerateValidationMessage(
            string expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            var tagBuilder = _htmlGenerator.GenerateValidationMessage(
                ViewContext,
                expression: expression,
                message: message,
                tag: tag,
                htmlAttributes: htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual HtmlString GenerateValidationSummary(
            bool excludePropertyErrors,
            string message,
            object htmlAttributes,
            string tag)
        {
            var tagBuilder = _htmlGenerator.GenerateValidationSummary(
                ViewContext,
                excludePropertyErrors,
                message,
                headerTag: tag,
                htmlAttributes: htmlAttributes);
            if (tagBuilder == null)
            {
                return HtmlString.Empty;
            }

            return tagBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        protected virtual string GenerateValue(string expression, object value, string format, bool useViewData)
        {
            var fullName = DefaultHtmlGenerator.GetFullHtmlFieldName(ViewContext, expression);
            var attemptedValue =
                (string)DefaultHtmlGenerator.GetModelStateValue(ViewContext, fullName, typeof(string));

            string resolvedValue;
            if (attemptedValue != null)
            {
                // case 1: if ModelState has a value then it's already formatted so ignore format string
                resolvedValue = attemptedValue;
            }
            else if (useViewData)
            {
                // case 2: format the value from ViewData
                resolvedValue = DefaultHtmlGenerator.EvalString(ViewContext, expression, format);
            }
            else
            {
                // case 3: format the explicit value from ModelMetadata
                resolvedValue = FormatValue(value, format);
            }

            return resolvedValue;
        }

        /// <inheritdoc />
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            ModelExplorer modelExplorer,
            string expression)
        {
            return _htmlGenerator.GetClientValidationRules(ViewContext, modelExplorer, expression);
        }

        /// <summary>
        /// Returns a select list for the given <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata"><see cref="ModelMetadata"/> to generate a select list for.</param>
        /// <returns>
        /// An <see cref="IEnumerable{SelectListItem}"/> containing the select list for the given
        /// <paramref name="metadata"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="metadata"/>'s <see cref="ModelMetadata.ModelType"/> is not an <see cref="Enum"/>
        /// or if it has a <see cref="FlagsAttribute"/>.
        /// </exception>
        protected virtual IEnumerable<SelectListItem> GetEnumSelectList([NotNull] ModelMetadata metadata)
        {
            if (!metadata.IsEnum || metadata.IsFlagsEnum)
            {
                var message = Resources.FormatHtmlHelper_TypeNotSupported_ForGetEnumSelectList(
                    metadata.ModelType.FullName,
                    nameof(Enum).ToLowerInvariant(),
                    nameof(FlagsAttribute));
                throw new ArgumentException(message, nameof(metadata));
            }

            var selectList = new List<SelectListItem>();
            foreach (var keyValuePair in metadata.EnumDisplayNamesAndValues)
            {
                var selectListItem = new SelectListItem
                {
                    Text = keyValuePair.Key,
                    Value = keyValuePair.Value,
                };

                selectList.Add(selectListItem);
            }

            return selectList;
        }
    }
}
