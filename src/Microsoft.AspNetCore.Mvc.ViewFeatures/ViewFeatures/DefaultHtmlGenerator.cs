// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class DefaultHtmlGenerator : IHtmlGenerator
    {
        private const string HiddenListItem = @"<li style=""display:none""></li>";
        private static readonly MethodInfo ConvertEnumFromStringMethod =
            typeof(DefaultHtmlGenerator).GetTypeInfo().GetDeclaredMethod(nameof(ConvertEnumFromString));

        // See: (http://www.w3.org/TR/html5/forms.html#the-input-element)
        private static readonly string[] _placeholderInputTypes =
            new[] { "text", "search", "url", "tel", "email", "password", "number" };

        private readonly IAntiforgery _antiforgery;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ValidationHtmlAttributeProvider _validationAttributeProvider;

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="DefaultHtmlGenerator"/> class.
        /// </para>
        /// <para>
        /// This constructor is obsolete and will be removed in a future version. The recommended alternative is to
        /// use <see cref="DefaultHtmlGenerator(IAntiforgery, IOptions{MvcViewOptions}, IModelMetadataProvider,
        /// IUrlHelperFactory, HtmlEncoder, ClientValidatorCache, ValidationHtmlAttributeProvider)"/>.
        /// </para>
        /// </summary>
        /// <param name="antiforgery">The <see cref="IAntiforgery"/> instance which is used to generate antiforgery
        /// tokens.</param>
        /// <param name="optionsAccessor">The accessor for <see cref="MvcViewOptions"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        /// <param name="clientValidatorCache">The <see cref="ClientValidatorCache"/> that provides
        /// a list of <see cref="IClientModelValidator"/>s.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended " +
            "alternative is to use the other public constructor.")]
        public DefaultHtmlGenerator(
            IAntiforgery antiforgery,
            IOptions<MvcViewOptions> optionsAccessor,
            IModelMetadataProvider metadataProvider,
            IUrlHelperFactory urlHelperFactory,
            HtmlEncoder htmlEncoder,
            ClientValidatorCache clientValidatorCache) : this(
                antiforgery,
                optionsAccessor,
                metadataProvider,
                urlHelperFactory,
                htmlEncoder,
                clientValidatorCache,
                new DefaultValidationHtmlAttributeProvider(optionsAccessor, metadataProvider, clientValidatorCache))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHtmlGenerator"/> class.
        /// </summary>
        /// <param name="antiforgery">The <see cref="IAntiforgery"/> instance which is used to generate antiforgery
        /// tokens.</param>
        /// <param name="optionsAccessor">The accessor for <see cref="MvcViewOptions"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        /// <param name="clientValidatorCache">The <see cref="ClientValidatorCache"/> that provides
        /// a list of <see cref="IClientModelValidator"/>s.</param>
        /// <param name="validationAttributeProvider">The <see cref="ValidationHtmlAttributeProvider"/>.</param>
        public DefaultHtmlGenerator(
            IAntiforgery antiforgery,
            IOptions<MvcViewOptions> optionsAccessor,
            IModelMetadataProvider metadataProvider,
            IUrlHelperFactory urlHelperFactory,
            HtmlEncoder htmlEncoder,
            ClientValidatorCache clientValidatorCache,
            ValidationHtmlAttributeProvider validationAttributeProvider)
        {
            if (antiforgery == null)
            {
                throw new ArgumentNullException(nameof(antiforgery));
            }

            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (urlHelperFactory == null)
            {
                throw new ArgumentNullException(nameof(urlHelperFactory));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            if (clientValidatorCache == null)
            {
                throw new ArgumentNullException(nameof(clientValidatorCache));
            }

            if (validationAttributeProvider == null)
            {
                throw new ArgumentNullException(nameof(validationAttributeProvider));
            }

            _antiforgery = antiforgery;
            _metadataProvider = metadataProvider;
            _urlHelperFactory = urlHelperFactory;
            _htmlEncoder = htmlEncoder;
            _validationAttributeProvider = validationAttributeProvider;

            // Underscores are fine characters in id's.
            IdAttributeDotReplacement = optionsAccessor.Value.HtmlHelperOptions.IdAttributeDotReplacement;
        }

        /// <inheritdoc />
        public string IdAttributeDotReplacement { get; }

        /// <inheritdoc />
        public string Encode(string value)
        {
            return !string.IsNullOrEmpty(value) ? _htmlEncoder.Encode(value) : string.Empty;
        }

        /// <inheritdoc />
        public string Encode(object value)
        {
            return (value != null) ? _htmlEncoder.Encode(value.ToString()) : string.Empty;
        }

        /// <inheritdoc />
        public string FormatValue(object value, string format)
        {
            return ViewDataDictionary.FormatValue(value, format);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateActionLink(
            ViewContext viewContext,
            string linkText,
            string actionName,
            string controllerName,
            string protocol,
            string hostname,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (linkText == null)
            {
                throw new ArgumentNullException(nameof(linkText));
            }

            var urlHelper = _urlHelperFactory.GetUrlHelper(viewContext);
            var url = urlHelper.Action(actionName, controllerName, routeValues, protocol, hostname, fragment);
            return GenerateLink(linkText, url, htmlAttributes);
        }

        /// <inheritdoc />
        public virtual IHtmlContent GenerateAntiforgery(ViewContext viewContext)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var formContext = viewContext.FormContext;
            if (formContext.CanRenderAtEndOfForm)
            {
                // Inside a BeginForm/BeginRouteForm or a <form> tag helper. So, the antiforgery token might have
                // already been created and appended to the 'end form' content (the AntiForgeryToken HTML helper does
                // this) OR the <form> tag helper might have already generated an antiforgery token.
                if (formContext.HasAntiforgeryToken)
                {
                    return HtmlString.Empty;
                }

                formContext.HasAntiforgeryToken = true;
            }

            return _antiforgery.GetHtml(viewContext.HttpContext);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateCheckBox(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            bool? isChecked,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (modelExplorer != null)
            {
                // CheckBoxFor() case. That API does not support passing isChecked directly.
                Debug.Assert(!isChecked.HasValue);

                if (modelExplorer.Model != null)
                {
                    bool modelChecked;
                    if (bool.TryParse(modelExplorer.Model.ToString(), out modelChecked))
                    {
                        isChecked = modelChecked;
                    }
                }
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            if (isChecked.HasValue && htmlAttributeDictionary != null)
            {
                // Explicit isChecked value must override "checked" in dictionary.
                htmlAttributeDictionary.Remove("checked");
            }

            // Use ViewData only in CheckBox case (metadata null) and when the user didn't pass an isChecked value.
            return GenerateInput(
                viewContext,
                InputType.CheckBox,
                modelExplorer,
                expression,
                value: "true",
                useViewData: (modelExplorer == null && !isChecked.HasValue),
                isChecked: isChecked ?? false,
                setId: true,
                isExplicitValue: false,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateHiddenForCheckbox(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttribute("type", GetInputTypeString(InputType.Hidden));
            tagBuilder.MergeAttribute("value", "false");
            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;

            var fullName = GetFullHtmlFieldName(viewContext, expression);
            tagBuilder.MergeAttribute("name", fullName);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateForm(
            ViewContext viewContext,
            string actionName,
            string controllerName,
            object routeValues,
            string method,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var defaultMethod = false;
            if (string.IsNullOrEmpty(method))
            {
                defaultMethod = true;
            }
            else if (string.Equals(method, "post", StringComparison.OrdinalIgnoreCase))
            {
                defaultMethod = true;
            }

            string action;
            if (actionName == null && controllerName == null && routeValues == null && defaultMethod)
            {
                // Submit to the original URL in the special case that user called the BeginForm() overload without
                // parameters (except for the htmlAttributes parameter). Also reachable in the even-more-unusual case
                // that user called another BeginForm() overload with default argument values.
                var request = viewContext.HttpContext.Request;
                action = request.PathBase + request.Path + request.QueryString;
            }
            else
            {
                var urlHelper = _urlHelperFactory.GetUrlHelper(viewContext);
                action = urlHelper.Action(action: actionName, controller: controllerName, values: routeValues);
            }

            return GenerateFormCore(viewContext, action, method, htmlAttributes);
        }

        /// <inheritdoc />
        public TagBuilder GenerateRouteForm(
            ViewContext viewContext,
            string routeName,
            object routeValues,
            string method,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var urlHelper = _urlHelperFactory.GetUrlHelper(viewContext);
            var action = urlHelper.RouteUrl(routeName, routeValues);

            return GenerateFormCore(viewContext, action, method, htmlAttributes);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateHidden(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool useViewData,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            // Special-case opaque values and arbitrary binary data.
            var byteArrayValue = value as byte[];
            if (byteArrayValue != null)
            {
                value = Convert.ToBase64String(byteArrayValue);
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            return GenerateInput(
                viewContext,
                InputType.Hidden,
                modelExplorer,
                expression,
                value,
                useViewData,
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateLabel(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            string labelText,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (modelExplorer == null)
            {
                throw new ArgumentNullException(nameof(modelExplorer));
            }

            var resolvedLabelText = labelText ??
                modelExplorer.Metadata.DisplayName ??
                modelExplorer.Metadata.PropertyName;
            if (resolvedLabelText == null && expression != null)
            {
                var index = expression.LastIndexOf('.');
                if (index == -1)
                {
                    // Expression does not contain a dot separator.
                    resolvedLabelText = expression;
                }
                else
                {
                    resolvedLabelText = expression.Substring(index + 1);
                }
            }

            if (string.IsNullOrEmpty(resolvedLabelText))
            {
                return null;
            }

            var tagBuilder = new TagBuilder("label");
            var idString =
                TagBuilder.CreateSanitizedId(GetFullHtmlFieldName(viewContext, expression), IdAttributeDotReplacement);
            tagBuilder.Attributes.Add("for", idString);
            tagBuilder.InnerHtml.SetContent(resolvedLabelText);
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes), replaceExisting: true);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GeneratePassword(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            return GenerateInput(
                viewContext,
                InputType.Password,
                modelExplorer,
                expression,
                value,
                useViewData: false,
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateRadioButton(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool? isChecked,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            if (modelExplorer == null)
            {
                // RadioButton() case. Do not override checked attribute if isChecked is implicit.
                if (!isChecked.HasValue &&
                    (htmlAttributeDictionary == null || !htmlAttributeDictionary.ContainsKey("checked")))
                {
                    // Note value may be null if isChecked is non-null.
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    // isChecked not provided nor found in the given attributes; fall back to view data.
                    var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
                    isChecked = string.Equals(
                        EvalString(viewContext, expression),
                        valueString,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                // RadioButtonFor() case. That API does not support passing isChecked directly.
                Debug.Assert(!isChecked.HasValue);

                // Need a value to determine isChecked.
                Debug.Assert(value != null);

                var model = modelExplorer.Model;
                var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);
                isChecked = model != null &&
                    string.Equals(model.ToString(), valueString, StringComparison.OrdinalIgnoreCase);
            }

            if (isChecked.HasValue && htmlAttributeDictionary != null)
            {
                // Explicit isChecked value must override "checked" in dictionary.
                htmlAttributeDictionary.Remove("checked");
            }

            return GenerateInput(
                viewContext,
                InputType.Radio,
                modelExplorer,
                expression,
                value,
                useViewData: false,
                isChecked: isChecked ?? false,
                setId: true,
                isExplicitValue: true,
                format: null,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateRouteLink(
            ViewContext viewContext,
            string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (linkText == null)
            {
                throw new ArgumentNullException(nameof(linkText));
            }

            var urlHelper = _urlHelperFactory.GetUrlHelper(viewContext);
            var url = urlHelper.RouteUrl(routeName, routeValues, protocol, hostName, fragment);
            return GenerateLink(linkText, url, htmlAttributes);
        }

        /// <inheritdoc />
        public TagBuilder GenerateSelect(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            bool allowMultiple,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var currentValues = GetCurrentValues(viewContext, modelExplorer, expression, allowMultiple);
            return GenerateSelect(
                viewContext,
                modelExplorer,
                optionLabel,
                expression,
                selectList,
                currentValues,
                allowMultiple,
                htmlAttributes);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateSelect(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            ICollection<string> currentValues,
            bool allowMultiple,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var fullName = GetFullHtmlFieldName(viewContext, expression);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(
                    Resources.FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(
                        typeof(IHtmlHelper).FullName,
                        nameof(IHtmlHelper.Editor),
                        typeof(IHtmlHelper<>).FullName,
                        nameof(IHtmlHelper<object>.EditorFor),
                        "htmlFieldName"),
                    nameof(expression));
            }

            // If we got a null selectList, try to use ViewData to get the list of items.
            if (selectList == null)
            {
                selectList = GetSelectListItems(viewContext, expression);
            }

            modelExplorer = modelExplorer ??
                ExpressionMetadataProvider.FromStringExpression(expression, viewContext.ViewData, _metadataProvider);

            // Convert each ListItem to an <option> tag and wrap them with <optgroup> if requested.
            var listItemBuilder = GenerateGroupsAndOptions(optionLabel, selectList, currentValues);

            var tagBuilder = new TagBuilder("select");
            tagBuilder.InnerHtml.SetHtmlContent(listItemBuilder);
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));
            tagBuilder.MergeAttribute("name", fullName, true /* replaceExisting */);
            tagBuilder.GenerateId(fullName, IdAttributeDotReplacement);
            if (allowMultiple)
            {
                tagBuilder.MergeAttribute("multiple", "multiple");
            }

            // If there are any errors for a named field, we add the css attribute.
            ModelStateEntry entry;
            if (viewContext.ViewData.ModelState.TryGetValue(fullName, out entry))
            {
                if (entry.Errors.Count > 0)
                {
                    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
                }
            }

            AddValidationAttributes(viewContext, tagBuilder, modelExplorer, expression);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateTextArea(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            int rows,
            int columns,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (rows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), Resources.HtmlHelper_TextAreaParameterOutOfRange);
            }

            if (columns < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(columns),
                    Resources.HtmlHelper_TextAreaParameterOutOfRange);
            }

            var fullName = GetFullHtmlFieldName(viewContext, expression);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(
                    Resources.FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(
                        typeof(IHtmlHelper).FullName,
                        nameof(IHtmlHelper.Editor),
                        typeof(IHtmlHelper<>).FullName,
                        nameof(IHtmlHelper<object>.EditorFor),
                        "htmlFieldName"),
                    nameof(expression));
            }

            ModelStateEntry entry;
            viewContext.ViewData.ModelState.TryGetValue(fullName, out entry);

            var value = string.Empty;
            if (entry != null && entry.AttemptedValue != null)
            {
                value = entry.AttemptedValue;
            }
            else if (modelExplorer.Model != null)
            {
                value = modelExplorer.Model.ToString();
            }

            var tagBuilder = new TagBuilder("textarea");
            tagBuilder.GenerateId(fullName, IdAttributeDotReplacement);
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes), true);
            if (rows > 0)
            {
                tagBuilder.MergeAttribute("rows", rows.ToString(CultureInfo.InvariantCulture), true);
            }

            if (columns > 0)
            {
                tagBuilder.MergeAttribute("cols", columns.ToString(CultureInfo.InvariantCulture), true);
            }

            tagBuilder.MergeAttribute("name", fullName, true);

            AddPlaceholderAttribute(viewContext.ViewData, tagBuilder, modelExplorer, expression);
            AddValidationAttributes(viewContext, tagBuilder, modelExplorer, expression);

            // If there are any errors for a named field, we add this CSS attribute.
            if (entry != null && entry.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }

            // The first newline is always trimmed when a TextArea is rendered, so we add an extra one
            // in case the value being rendered is something like "\r\nHello"
            tagBuilder.InnerHtml.AppendLine();
            tagBuilder.InnerHtml.Append(value);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateTextBox(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            string format,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            return GenerateInput(
                viewContext,
                InputType.Text,
                modelExplorer,
                expression,
                value,
                useViewData: (modelExplorer == null && value == null),
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: format,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateValidationMessage(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            string message,
            string tag,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var fullName = GetFullHtmlFieldName(viewContext, expression);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(
                    Resources.FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(
                        typeof(IHtmlHelper).FullName,
                        nameof(IHtmlHelper.Editor),
                        typeof(IHtmlHelper<>).FullName,
                        nameof(IHtmlHelper<object>.EditorFor),
                        "htmlFieldName"),
                    nameof(expression));
            }

            var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
            if (!viewContext.ViewData.ModelState.ContainsKey(fullName) && formContext == null)
            {
                return null;
            }

            ModelStateEntry entry;
            var tryGetModelStateResult = viewContext.ViewData.ModelState.TryGetValue(fullName, out entry);
            var modelErrors = tryGetModelStateResult ? entry.Errors : null;

            ModelError modelError = null;
            if (modelErrors != null && modelErrors.Count != 0)
            {
                modelError = modelErrors.FirstOrDefault(m => !string.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0];
            }

            if (modelError == null && formContext == null)
            {
                return null;
            }

            // Even if there are no model errors, we generate the span and add the validation message
            // if formContext is not null.
            if (string.IsNullOrEmpty(tag))
            {
                tag = viewContext.ValidationMessageElement;
            }
            var tagBuilder = new TagBuilder(tag);
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));

            // Only the style of the span is changed according to the errors if message is null or empty.
            // Otherwise the content and style is handled by the client-side validation.
            var className = (modelError != null) ?
                HtmlHelper.ValidationMessageCssClassName :
                HtmlHelper.ValidationMessageValidCssClassName;
            tagBuilder.AddCssClass(className);

            if (!string.IsNullOrEmpty(message))
            {
                tagBuilder.InnerHtml.SetContent(message);
            }
            else if (modelError != null)
            {
                modelExplorer = modelExplorer ?? ExpressionMetadataProvider.FromStringExpression(
                    expression,
                    viewContext.ViewData,
                    _metadataProvider);
                tagBuilder.InnerHtml.SetContent(
                    ValidationHelpers.GetModelErrorMessageOrDefault(modelError, entry, modelExplorer));
            }

            if (formContext != null)
            {
                tagBuilder.MergeAttribute("data-valmsg-for", fullName);

                var replaceValidationMessageContents = string.IsNullOrEmpty(message);
                tagBuilder.MergeAttribute("data-valmsg-replace",
                    replaceValidationMessageContents.ToString().ToLowerInvariant());
            }

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateValidationSummary(
            ViewContext viewContext,
            bool excludePropertyErrors,
            string message,
            string headerTag,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var viewData = viewContext.ViewData;
            if (!viewContext.ClientValidationEnabled && viewData.ModelState.IsValid)
            {
                // Client-side validation is not enabled to add to the generated element and element will be empty.
                return null;
            }

            ModelStateEntry entryForModel;
            if (excludePropertyErrors &&
                (!viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out entryForModel) ||
                 entryForModel.Errors.Count == 0))
            {
                // Client-side validation (if enabled) will not affect the generated element and element will be empty.
                return null;
            }

            TagBuilder messageTag;
            if (string.IsNullOrEmpty(message))
            {
                messageTag = null;
            }
            else
            {
                if (string.IsNullOrEmpty(headerTag))
                {
                    headerTag = viewContext.ValidationSummaryMessageElement;
                }

                messageTag = new TagBuilder(headerTag);
                messageTag.InnerHtml.SetContent(message);
            }

            // If excludePropertyErrors is true, describe any validation issue with the current model in a single item.
            // Otherwise, list individual property errors.
            var isHtmlSummaryModified = false;
            var modelStates = ValidationHelpers.GetModelStateList(viewData, excludePropertyErrors);

            var htmlSummary = new TagBuilder("ul");
            foreach (var modelState in modelStates)
            {
                // Perf: Avoid allocations
                for (var i = 0; i < modelState.Errors.Count; i++)
                {
                    var modelError = modelState.Errors[i];
                    var errorText = ValidationHelpers.GetModelErrorMessageOrDefault(modelError);

                    if (!string.IsNullOrEmpty(errorText))
                    {
                        var listItem = new TagBuilder("li");
                        listItem.InnerHtml.SetContent(errorText);
                        htmlSummary.InnerHtml.AppendLine(listItem);
                        isHtmlSummaryModified = true;
                    }
                }
            }

            if (!isHtmlSummaryModified)
            {
                htmlSummary.InnerHtml.AppendHtml(HiddenListItem);
                htmlSummary.InnerHtml.AppendLine();
            }

            var tagBuilder = new TagBuilder("div");
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));

            if (viewData.ModelState.IsValid)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationSummaryValidCssClassName);
            }
            else
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationSummaryCssClassName);
            }

            if (messageTag != null)
            {
                tagBuilder.InnerHtml.AppendLine(messageTag);
            }

            tagBuilder.InnerHtml.AppendHtml(htmlSummary);

            if (viewContext.ClientValidationEnabled && !excludePropertyErrors)
            {
                // Inform the client where to replace the list of property errors after validation.
                tagBuilder.MergeAttribute("data-valmsg-summary", "true");
            }

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual ICollection<string> GetCurrentValues(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            bool allowMultiple)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var fullName = GetFullHtmlFieldName(viewContext, expression);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(
                    Resources.FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(
                        typeof(IHtmlHelper).FullName,
                        nameof(IHtmlHelper.Editor),
                        typeof(IHtmlHelper<>).FullName,
                        nameof(IHtmlHelper<object>.EditorFor),
                        "htmlFieldName"),
                    nameof(expression));
            }

            var type = allowMultiple ? typeof(string[]) : typeof(string);
            var rawValue = GetModelStateValue(viewContext, fullName, type);

            // If ModelState did not contain a current value, fall back to ViewData- or ModelExplorer-supplied value.
            if (rawValue == null)
            {
                if (modelExplorer == null)
                {
                    // Html.DropDownList() and Html.ListBox() helper case.
                    rawValue = viewContext.ViewData.Eval(expression);
                    if (rawValue is IEnumerable<SelectListItem>)
                    {
                        // This ViewData item contains the fallback selectList collection for GenerateSelect().
                        // Do not try to use this collection.
                        rawValue = null;
                    }
                }
                else
                {
                    // <select/>, Html.DropDownListFor() and Html.ListBoxFor() helper case. Do not use ViewData.
                    rawValue = modelExplorer.Model;
                }

                if (rawValue == null)
                {
                    return null;
                }
            }

            // Convert raw value to a collection.
            IEnumerable rawValues;
            if (allowMultiple)
            {
                rawValues = rawValue as IEnumerable;
                if (rawValues == null || rawValues is string)
                {
                    throw new InvalidOperationException(
                        Resources.FormatHtmlHelper_SelectExpressionNotEnumerable(nameof(expression)));
                }
            }
            else
            {
                rawValues = new[] { rawValue };
            }

            modelExplorer = modelExplorer ??
                ExpressionMetadataProvider.FromStringExpression(expression, viewContext.ViewData, _metadataProvider);
            var metadata = modelExplorer.Metadata;
            if (allowMultiple && metadata.IsEnumerableType)
            {
                metadata = metadata.ElementMetadata;
            }

            var enumNames = metadata.EnumNamesAndValues;
            var isTargetEnum = metadata.IsEnum;

            // Logic below assumes isTargetEnum and enumNames are consistent. Confirm that expectation is met.
            Debug.Assert(isTargetEnum ^ enumNames == null);

            var innerType = metadata.UnderlyingOrModelType;

            // Convert raw value collection to strings.
            var currentValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in rawValues)
            {
                // Add original or converted string.
                var stringValue = (value as string) ?? Convert.ToString(value, CultureInfo.CurrentCulture);

                // Do not add simple names of enum properties here because whitespace isn't relevant for their binding.
                // Will add matching names just below.
                if (enumNames == null || !enumNames.ContainsKey(stringValue.Trim()))
                {
                    currentValues.Add(stringValue);
                }

                // Remainder handles isEnum cases. Convert.ToString() returns field names for enum values but select
                // list may (well, should) contain integer values.
                var enumValue = value as Enum;
                if (isTargetEnum && enumValue == null && value != null)
                {
                    var valueType = value.GetType();
                    if (typeof(long).IsAssignableFrom(valueType) || typeof(ulong).IsAssignableFrom(valueType))
                    {
                        // E.g. user added an int to a ViewData entry and called a string-based HTML helper.
                        enumValue = ConvertEnumFromInteger(value, innerType);
                    }
                    else if (!string.IsNullOrEmpty(stringValue))
                    {
                        // E.g. got a string from ModelState.
                        var methodInfo = ConvertEnumFromStringMethod.MakeGenericMethod(innerType);
                        enumValue = (Enum)methodInfo.Invoke(obj: null, parameters: new[] { stringValue });
                    }
                }

                if (enumValue != null)
                {
                    // Add integer value.
                    var integerString = enumValue.ToString("d");
                    currentValues.Add(integerString);

                    // isTargetEnum may be false when raw value has a different type than the target e.g. ViewData
                    // contains enum values and property has type int or string.
                    if (isTargetEnum)
                    {
                        // Add all simple names for this value.
                        var matchingNames = enumNames
                            .Where(kvp => string.Equals(integerString, kvp.Value, StringComparison.Ordinal))
                            .Select(kvp => kvp.Key);
                        foreach (var name in matchingNames)
                        {
                            currentValues.Add(name);
                        }
                    }
                }
            }

            return currentValues;
        }

        internal static string EvalString(ViewContext viewContext, string key, string format)
        {
            return Convert.ToString(viewContext.ViewData.Eval(key, format), CultureInfo.CurrentCulture);
        }

        /// <remarks>
        /// Not used directly in HtmlHelper. Exposed for use in DefaultDisplayTemplates.
        /// </remarks>
        internal static TagBuilder GenerateOption(SelectListItem item, string text)
        {
            return GenerateOption(item, text, item.Selected);
        }

        internal static TagBuilder GenerateOption(SelectListItem item, string text, bool selected)
        {
            var tagBuilder = new TagBuilder("option");
            tagBuilder.InnerHtml.SetContent(text);

            if (item.Value != null)
            {
                tagBuilder.Attributes["value"] = item.Value;
            }

            if (selected)
            {
                tagBuilder.Attributes["selected"] = "selected";
            }

            if (item.Disabled)
            {
                tagBuilder.Attributes["disabled"] = "disabled";
            }

            return tagBuilder;
        }

        internal static string GetFullHtmlFieldName(ViewContext viewContext, string expression)
        {
            var fullName = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
            return fullName;
        }

        internal static object GetModelStateValue(ViewContext viewContext, string key, Type destinationType)
        {
            ModelStateEntry entry;
            if (viewContext.ViewData.ModelState.TryGetValue(key, out entry) && entry.RawValue != null)
            {
                return ModelBindingHelper.ConvertTo(entry.RawValue, destinationType, culture: null);
            }

            return null;
        }

        /// <summary>
        /// Generate a &lt;form&gt; element.
        /// </summary>
        /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
        /// <param name="action">The URL where the form-data should be submitted.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
        /// </returns>
        protected virtual TagBuilder GenerateFormCore(
            ViewContext viewContext,
            string action,
            string method,
            object htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var tagBuilder = new TagBuilder("form");
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));

            // action is implicitly generated from other parameters, so htmlAttributes take precedence.
            tagBuilder.MergeAttribute("action", action);

            if (string.IsNullOrEmpty(method))
            {
                // Occurs only when called from a tag helper.
                method = "post";
            }

            // For tag helpers, htmlAttributes will be null; replaceExisting value does not matter.
            // method is an explicit parameter to HTML helpers, so it takes precedence over the htmlAttributes.
            tagBuilder.MergeAttribute("method", method, replaceExisting: true);

            return tagBuilder;
        }

        protected virtual TagBuilder GenerateInput(
            ViewContext viewContext,
            InputType inputType,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool useViewData,
            bool isChecked,
            bool setId,
            bool isExplicitValue,
            string format,
            IDictionary<string, object> htmlAttributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            // Not valid to use TextBoxForModel() and so on in a top-level view; would end up with an unnamed input
            // elements. But we support the *ForModel() methods in any lower-level template, once HtmlFieldPrefix is
            // non-empty.
            var fullName = GetFullHtmlFieldName(viewContext, expression);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(
                    Resources.FormatHtmlGenerator_FieldNameCannotBeNullOrEmpty(
                        typeof(IHtmlHelper).FullName,
                        nameof(IHtmlHelper.Editor),
                        typeof(IHtmlHelper<>).FullName,
                        nameof(IHtmlHelper<object>.EditorFor),
                        "htmlFieldName"),
                    nameof(expression));
            }

            var inputTypeString = GetInputTypeString(inputType);
            var tagBuilder = new TagBuilder("input");
            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("type", inputTypeString);
            tagBuilder.MergeAttribute("name", fullName, replaceExisting: true);

            var suppliedTypeString = tagBuilder.Attributes["type"];
            if (_placeholderInputTypes.Contains(suppliedTypeString))
            {
                AddPlaceholderAttribute(viewContext.ViewData, tagBuilder, modelExplorer, expression);
            }

            var valueParameter = FormatValue(value, format);
            var usedModelState = false;
            switch (inputType)
            {
                case InputType.CheckBox:
                    var modelStateWasChecked = GetModelStateValue(viewContext, fullName, typeof(bool)) as bool?;
                    if (modelStateWasChecked.HasValue)
                    {
                        isChecked = modelStateWasChecked.Value;
                        usedModelState = true;
                    }

                    goto case InputType.Radio;

                case InputType.Radio:
                    if (!usedModelState)
                    {
                        var modelStateValue = GetModelStateValue(viewContext, fullName, typeof(string)) as string;
                        if (modelStateValue != null)
                        {
                            isChecked = string.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                            usedModelState = true;
                        }
                    }

                    if (!usedModelState && useViewData)
                    {
                        isChecked = EvalBoolean(viewContext, expression);
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
                    var attributeValue = (string)GetModelStateValue(viewContext, fullName, typeof(string));
                    if (attributeValue == null)
                    {
                        attributeValue = useViewData ? EvalString(viewContext, expression, format) : valueParameter;
                    }

                    var addValue = true;
                    object typeAttributeValue;
                    if (htmlAttributes != null && htmlAttributes.TryGetValue("type", out typeAttributeValue))
                    {
                        var typeAttributeString = typeAttributeValue.ToString();
                        if (string.Equals(typeAttributeString, "file", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(typeAttributeString, "image", StringComparison.OrdinalIgnoreCase))
                        {
                            // 'value' attribute is not needed for 'file' and 'image' input types.
                            addValue = false;
                        }
                    }

                    if (addValue)
                    {
                        tagBuilder.MergeAttribute("value", attributeValue, replaceExisting: isExplicitValue);
                    }

                    break;
            }

            if (setId)
            {
                tagBuilder.GenerateId(fullName, IdAttributeDotReplacement);
            }

            // If there are any errors for a named field, we add the CSS attribute.
            ModelStateEntry entry;
            if (viewContext.ViewData.ModelState.TryGetValue(fullName, out entry) && entry.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }

            AddValidationAttributes(viewContext, tagBuilder, modelExplorer, expression);

            return tagBuilder;
        }

        protected virtual TagBuilder GenerateLink(
            string linkText,
            string url,
            object htmlAttributes)
        {
            if (linkText == null)
            {
                throw new ArgumentNullException(nameof(linkText));
            }

            var tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.SetContent(linkText);

            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));
            tagBuilder.MergeAttribute("href", url);

            return tagBuilder;
        }

        /// <summary>
        /// Adds a placeholder attribute to the <paramref name="tagBuilder" />.
        /// </summary>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> instance for the current scope.</param>
        /// <param name="tagBuilder">A <see cref="TagBuilder"/> instance.</param>
        /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        protected virtual void AddPlaceholderAttribute(
            ViewDataDictionary viewData,
            TagBuilder tagBuilder,
            ModelExplorer modelExplorer,
            string expression)
        {
            modelExplorer = modelExplorer ?? ExpressionMetadataProvider.FromStringExpression(
                expression,
                viewData,
                _metadataProvider);

            var placeholder = modelExplorer.Metadata.Placeholder;
            if (!string.IsNullOrEmpty(placeholder))
            {
                tagBuilder.MergeAttribute("placeholder", placeholder);
            }
        }

        /// <summary>
        /// Adds validation attributes to the <paramref name="tagBuilder" /> if client validation
        /// is enabled.
        /// </summary>
        /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
        /// <param name="tagBuilder">A <see cref="TagBuilder"/> instance.</param>
        /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        protected virtual void AddValidationAttributes(
            ViewContext viewContext,
            TagBuilder tagBuilder,
            ModelExplorer modelExplorer,
            string expression)
        {
            modelExplorer = modelExplorer ?? ExpressionMetadataProvider.FromStringExpression(
                expression,
                viewContext.ViewData,
                _metadataProvider);

            _validationAttributeProvider.AddAndTrackValidationAttributes(
                viewContext,
                modelExplorer,
                expression,
                tagBuilder.Attributes);
        }

        private static Enum ConvertEnumFromInteger(object value, Type targetType)
        {
            try
            {
                return (Enum)Enum.ToObject(targetType, value);
            }
            catch (Exception exception)
            when (exception is FormatException || exception.InnerException is FormatException)
            {
                // The integer was too large for this enum type.
                return null;
            }
        }

        private static object ConvertEnumFromString<TEnum>(string value) where TEnum : struct
        {
            TEnum enumValue;
            if (Enum.TryParse(value, out enumValue))
            {
                return enumValue;
            }

            // Do not return default(TEnum) when parse was unsuccessful.
            return null;
        }

        private static bool EvalBoolean(ViewContext viewContext, string key)
        {
            return Convert.ToBoolean(viewContext.ViewData.Eval(key), CultureInfo.InvariantCulture);
        }

        private static string EvalString(ViewContext viewContext, string key)
        {
            return Convert.ToString(viewContext.ViewData.Eval(key), CultureInfo.CurrentCulture);
        }

        // Only need a dictionary if htmlAttributes is non-null. TagBuilder.MergeAttributes() is fine with null.
        private static IDictionary<string, object> GetHtmlAttributeDictionaryOrNull(object htmlAttributes)
        {
            IDictionary<string, object> htmlAttributeDictionary = null;
            if (htmlAttributes != null)
            {
                htmlAttributeDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributeDictionary == null)
                {
                    htmlAttributeDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            return htmlAttributeDictionary;
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

        private static IEnumerable<SelectListItem> GetSelectListItems(
            ViewContext viewContext,
            string expression)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            // Method is called only if user did not pass a select list in. They must provide select list items in the
            // ViewData dictionary and definitely not as the Model. (Even if the Model datatype were correct, a
            // <select> element generated for a collection of SelectListItems would be useless.)
            var value = viewContext.ViewData.Eval(expression);

            // First check whether above evaluation was successful and did not match ViewData.Model.
            if (value == null || value == viewContext.ViewData.Model)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_MissingSelectData(
                    $"IEnumerable<{nameof(SelectListItem)}>",
                    expression));
            }

            // Second check the Eval() call returned a collection of SelectListItems.
            var selectList = value as IEnumerable<SelectListItem>;
            if (selectList == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_WrongSelectDataType(
                    expression,
                    value.GetType().FullName,
                    $"IEnumerable<{nameof(SelectListItem)}>"));
            }

            return selectList;
        }

        /// <inheritdoc />
        public IHtmlContent GenerateGroupsAndOptions(string optionLabel, IEnumerable<SelectListItem> selectList)
        {
            return GenerateGroupsAndOptions(optionLabel: optionLabel, selectList: selectList, currentValues: null);
        }

        private IHtmlContent GenerateGroupsAndOptions(
            string optionLabel,
            IEnumerable<SelectListItem> selectList,
            ICollection<string> currentValues)
        {
            var itemsList = selectList as IList<SelectListItem>;
            if (itemsList == null)
            {
                itemsList = selectList.ToList();
            }

            var count = itemsList.Count;
            if (optionLabel != null)
            {
                count++;
            }

            // Short-circuit work below if there's nothing to add.
            if (count == 0)
            {
                return HtmlString.Empty;
            }

            var listItemBuilder = new HtmlContentBuilder(count);

            // Make optionLabel the first item that gets rendered.
            if (optionLabel != null)
            {
                listItemBuilder.AppendLine(GenerateOption(
                    new SelectListItem()
                    {
                        Text = optionLabel,
                        Value = string.Empty,
                        Selected = false,
                    },
                    currentValues: null));
            }

            // Group items in the SelectList if requested.
            // The worst case complexity of this algorithm is O(number of groups*n).
            // If there aren't any groups, it is O(n) where n is number of items in the list.
            var optionGenerated = new bool[itemsList.Count];
            for (var i = 0; i < itemsList.Count; i++)
            {
                if (!optionGenerated[i])
                {
                    var item = itemsList[i];
                    var optGroup = item.Group;
                    if (optGroup != null)
                    {
                        var groupBuilder = new TagBuilder("optgroup");
                        if (optGroup.Name != null)
                        {
                            groupBuilder.MergeAttribute("label", optGroup.Name);
                        }

                        if (optGroup.Disabled)
                        {
                            groupBuilder.MergeAttribute("disabled", "disabled");
                        }

                        groupBuilder.InnerHtml.AppendLine();

                        for (var j = i; j < itemsList.Count; j++)
                        {
                            var groupItem = itemsList[j];

                            if (!optionGenerated[j] &&
                                object.ReferenceEquals(optGroup, groupItem.Group))
                            {
                                groupBuilder.InnerHtml.AppendLine(GenerateOption(groupItem, currentValues));
                                optionGenerated[j] = true;
                            }
                        }

                        listItemBuilder.AppendLine(groupBuilder);
                    }
                    else
                    {
                        listItemBuilder.AppendLine(GenerateOption(item, currentValues));
                        optionGenerated[i] = true;
                    }
                }
            }

            return listItemBuilder;
        }

        private IHtmlContent GenerateOption(SelectListItem item, ICollection<string> currentValues)
        {
            var selected = item.Selected;
            if (currentValues != null)
            {
                var value = item.Value ?? item.Text;
                selected = currentValues.Contains(value);
            }

            var tagBuilder = GenerateOption(item, item.Text, selected);
            return tagBuilder;
        }
    }
}
