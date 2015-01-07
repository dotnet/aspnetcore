// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class DefaultHtmlGenerator : IHtmlGenerator
    {
        private const string HiddenListItem = @"<li style=""display:none""></li>";

        private readonly IActionBindingContextProvider _actionBindingContextProvider;
        private readonly AntiForgery _antiForgery;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IUrlHelper _urlHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHtmlGenerator"/> class.
        /// </summary>
        public DefaultHtmlGenerator(
            [NotNull] IActionBindingContextProvider actionBindingContextProvider,
            [NotNull] AntiForgery antiForgery,
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] IUrlHelper urlHelper)
        {
            _actionBindingContextProvider = actionBindingContextProvider;
            _antiForgery = antiForgery;
            _metadataProvider = metadataProvider;
            _urlHelper = urlHelper;

            // Underscores are fine characters in id's.
            IdAttributeDotReplacement = "_";
        }

        /// <inheritdoc />
        public string IdAttributeDotReplacement { get; set; }

        /// <inheritdoc />
        public string Encode(string value)
        {
            return !string.IsNullOrEmpty(value) ? WebUtility.HtmlEncode(value) : string.Empty;
        }

        /// <inheritdoc />
        public string Encode(object value)
        {
            return (value != null) ? WebUtility.HtmlEncode(value.ToString()) : string.Empty;
        }

        /// <inheritdoc />
        public string FormatValue(object value, string format)
        {
            return ViewDataDictionary.FormatValue(value, format);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateActionLink(
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            string protocol,
            string hostname,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            var url = _urlHelper.Action(actionName, controllerName, routeValues, protocol, hostname, fragment);
            return GenerateLink(linkText, url, htmlAttributes);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateAntiForgery([NotNull] ViewContext viewContext)
        {
            var tagBuilder = _antiForgery.GetHtml(viewContext.HttpContext);
            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateCheckBox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            bool? isChecked,
            object htmlAttributes)
        {
            if (metadata != null)
            {
                // CheckBoxFor() case. That API does not support passing isChecked directly.
                Debug.Assert(!isChecked.HasValue);

                if (metadata.Model != null)
                {
                    bool modelChecked;
                    if (Boolean.TryParse(metadata.Model.ToString(), out modelChecked))
                    {
                        isChecked = modelChecked;
                    }
                }
            }

            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            var explicitValue = isChecked.HasValue;
            if (explicitValue && htmlAttributeDictionary != null)
            {
                // Explicit value must override dictionary
                htmlAttributeDictionary.Remove("checked");
            }

            return GenerateInput(
                viewContext,
                InputType.CheckBox,
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

        /// <inheritdoc />
        public virtual TagBuilder GenerateHiddenForCheckbox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name)
        {
            var tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttribute("type", GetInputTypeString(InputType.Hidden));
            tagBuilder.MergeAttribute("value", "false");

            var fullName = GetFullHtmlFieldName(viewContext, name);
            tagBuilder.MergeAttribute("name", fullName);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateForm(
            [NotNull] ViewContext viewContext,
            string actionName,
            string controllerName,
            object routeValues,
            string method,
            object htmlAttributes)
        {
            var defaultMethod = false;
            if (string.IsNullOrEmpty(method))
            {
                defaultMethod = true;
            }
            else if (string.Equals(method, FormMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                defaultMethod = true;
            }

            string action;
            if (actionName == null && controllerName == null && routeValues == null && defaultMethod &&
                htmlAttributes == null)
            {
                // Submit to the original URL in the special case that user called the BeginForm() overload without
                // parameters. Also reachable in the even-more-unusual case that user called another BeginForm()
                // overload with default argument values.
                var request = viewContext.HttpContext.Request;
                action = request.PathBase + request.Path + request.QueryString;
            }
            else
            {
                action = _urlHelper.Action(action: actionName, controller: controllerName, values: routeValues);
            }

            return GenerateFormCore(viewContext, action, method, htmlAttributes);
        }

        /// <inheritdoc />
        public TagBuilder GenerateRouteForm(
            [NotNull] ViewContext viewContext,
            string routeName,
            object routeValues,
            string method,
            object htmlAttributes)
        {
            var action =
                _urlHelper.RouteUrl(routeName, values: routeValues, protocol: null, host: null, fragment: null);

            return GenerateFormCore(viewContext, action, method, htmlAttributes);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateHidden(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            object value,
            bool useViewData,
            object htmlAttributes)
        {
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

        /// <inheritdoc />
        public virtual TagBuilder GenerateLabel(
            [NotNull] ViewContext viewContext,
            [NotNull] ModelMetadata metadata,
            string name,
            string labelText,
            object htmlAttributes)
        {
            var resolvedLabelText = labelText ?? metadata.DisplayName ?? metadata.PropertyName;
            if (resolvedLabelText == null)
            {
                resolvedLabelText =
                    string.IsNullOrEmpty(name) ? string.Empty : name.Split('.').Last();
            }

            if (string.IsNullOrEmpty(resolvedLabelText))
            {
                return null;
            }

            var tagBuilder = new TagBuilder("label");
            var idString =
                TagBuilder.CreateSanitizedId(GetFullHtmlFieldName(viewContext, name), IdAttributeDotReplacement);
            tagBuilder.Attributes.Add("for", idString);
            tagBuilder.SetInnerText(resolvedLabelText);
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes), replaceExisting: true);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GeneratePassword(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            object value,
            object htmlAttributes)
        {
            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            return GenerateInput(
                viewContext,
                InputType.Password,
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

        /// <inheritdoc />
        public virtual TagBuilder GenerateRadioButton(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            object value,
            bool? isChecked,
            object htmlAttributes)
        {
            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
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
                        string.Equals(EvalString(viewContext, name), valueString, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                // RadioButtonFor() case. That API does not support passing isChecked directly.
                Debug.Assert(!isChecked.HasValue);

                // Need a value to determine isChecked.
                Debug.Assert(value != null);

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

            return GenerateInput(
                viewContext,
                InputType.Radio,
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

        /// <inheritdoc />
        public virtual TagBuilder GenerateRouteLink(
            [NotNull] string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            var url = _urlHelper.RouteUrl(routeName, routeValues, protocol, hostName, fragment);
            return GenerateLink(linkText, url, htmlAttributes);
        }

        /// <inheritdoc />
        public TagBuilder GenerateSelect(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string optionLabel,
            string name,
            IEnumerable<SelectListItem> selectList,
            bool allowMultiple,
            object htmlAttributes)
        {
            ICollection<string> ignored;
            return GenerateSelect(
                viewContext,
                metadata,
                optionLabel,
                name,
                selectList,
                allowMultiple,
                htmlAttributes,
                selectedValues: out ignored);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateSelect(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string optionLabel,
            string name,
            IEnumerable<SelectListItem> selectList,
            bool allowMultiple,
            object htmlAttributes,
            out ICollection<string> selectedValues)
        {
            var fullName = GetFullHtmlFieldName(viewContext, name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "name");
            }

            // If we got a null selectList, try to use ViewData to get the list of items.
            var usedViewData = false;
            if (selectList == null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    // Avoid ViewData.Eval() throwing an ArgumentException with a different parameter name. Note this
                    // is an extreme case since users must pass a non-null selectList to use CheckBox() or ListBox()
                    // in a template, where a null or empty name has meaning.
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "name");
                }

                selectList = GetSelectListItems(viewContext, name);
                usedViewData = true;
            }

            var type = allowMultiple ? typeof(string[]) : typeof(string);
            var defaultValue = GetModelStateValue(viewContext, fullName, type);

            // If we haven't already used ViewData to get the entire list of items then we need to
            // use the ViewData-supplied value before using the parameter-supplied value.
            if (defaultValue == null && !string.IsNullOrEmpty(name))
            {
                if (!usedViewData)
                {
                    defaultValue = viewContext.ViewData.Eval(name);
                }
                else if (metadata != null)
                {
                    defaultValue = metadata.Model;
                }
            }

            if (defaultValue != null)
            {
                selectList =
                    UpdateSelectListItemsWithDefaultValue(selectList, defaultValue, allowMultiple, out selectedValues);
            }
            else
            {
                selectedValues = new string[0];
            }

            // Convert each ListItem to an <option> tag and wrap them with <optgroup> if requested.
            var listItemBuilder = GenerateGroupsAndOptions(optionLabel, selectList);

            var tagBuilder = new TagBuilder("select")
            {
                InnerHtml = listItemBuilder.ToString()
            };
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));
            tagBuilder.MergeAttribute("name", fullName, true /* replaceExisting */);
            tagBuilder.GenerateId(fullName, IdAttributeDotReplacement);
            if (allowMultiple)
            {
                tagBuilder.MergeAttribute("multiple", "multiple");
            }

            // If there are any errors for a named field, we add the css attribute.
            ModelState modelState;
            if (viewContext.ViewData.ModelState.TryGetValue(fullName, out modelState))
            {
                if (modelState.Errors.Count > 0)
                {
                    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
                }
            }

            tagBuilder.MergeAttributes(GetValidationAttributes(viewContext, metadata, name));

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateTextArea(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            int rows,
            int columns,
            object htmlAttributes)
        {
            if (rows < 0)
            {
                throw new ArgumentOutOfRangeException("rows", Resources.HtmlHelper_TextAreaParameterOutOfRange);
            }

            if (columns < 0)
            {
                throw new ArgumentOutOfRangeException("columns", Resources.HtmlHelper_TextAreaParameterOutOfRange);
            }

            var fullName = GetFullHtmlFieldName(viewContext, name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "name");
            }

            ModelState modelState;
            viewContext.ViewData.ModelState.TryGetValue(fullName, out modelState);

            var value = string.Empty;
            if (modelState != null && modelState.Value != null)
            {
                value = modelState.Value.AttemptedValue;
            }
            else if (metadata.Model != null)
            {
                value = metadata.Model.ToString();
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
                tagBuilder.MergeAttribute("columns", columns.ToString(CultureInfo.InvariantCulture), true);
            }

            tagBuilder.MergeAttribute("name", fullName, true);
            tagBuilder.MergeAttributes(GetValidationAttributes(viewContext, metadata, name));

            // If there are any errors for a named field, we add this CSS attribute.
            if (modelState != null && modelState.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }

            // The first newline is always trimmed when a TextArea is rendered, so we add an extra one
            // in case the value being rendered is something like "\r\nHello".
            tagBuilder.InnerHtml = Environment.NewLine + WebUtility.HtmlEncode(value);

            return tagBuilder;
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateTextBox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name,
            object value,
            string format,
            object htmlAttributes)
        {
            var htmlAttributeDictionary = GetHtmlAttributeDictionaryOrNull(htmlAttributes);
            return GenerateInput(
                viewContext,
                InputType.Text,
                metadata,
                name,
                value,
                useViewData: (metadata == null && value == null),
                isChecked: false,
                setId: true,
                isExplicitValue: true,
                format: format,
                htmlAttributes: htmlAttributeDictionary);
        }

        /// <inheritdoc />
        public virtual TagBuilder GenerateValidationMessage(
            [NotNull] ViewContext viewContext,
            string name,
            string message,
            string tag,
            object htmlAttributes)
        {
            var fullName = GetFullHtmlFieldName(viewContext, name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "expression");
            }

            var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
            if (!viewContext.ViewData.ModelState.ContainsKey(fullName) && formContext == null)
            {
                return null;
            }

            ModelState modelState;
            var tryGetModelStateResult = viewContext.ViewData.ModelState.TryGetValue(fullName, out modelState);
            var modelErrors = tryGetModelStateResult ? modelState.Errors : null;

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
                tagBuilder.SetInnerText(message);
            }
            else if (modelError != null)
            {
                tagBuilder.SetInnerText(ValidationHelpers.GetUserErrorMessageOrDefault(modelError, modelState));
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
            [NotNull] ViewContext viewContext,
            bool excludePropertyErrors,
            string message,
            string headerTag,
            object htmlAttributes)
        {
            var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
            if (viewContext.ViewData.ModelState.IsValid && (formContext == null || excludePropertyErrors))
            {
                // No client side validation/updates
                return null;
            }

            string wrappedMessage;
            if (!string.IsNullOrEmpty(message))
            {
                if (string.IsNullOrEmpty(headerTag))
                {
                    headerTag = viewContext.ValidationSummaryMessageElement;
                }
                var messageTag = new TagBuilder(headerTag);
                messageTag.SetInnerText(message);
                wrappedMessage = messageTag.ToString(TagRenderMode.Normal) + Environment.NewLine;
            }
            else
            {
                wrappedMessage = null;
            }

            // If excludePropertyErrors is true, describe any validation issue with the current model in a single item.
            // Otherwise, list individual property errors.
            var htmlSummary = new StringBuilder();
            var modelStates = ValidationHelpers.GetModelStateList(viewContext.ViewData, excludePropertyErrors);

            foreach (var modelState in modelStates)
            {
                foreach (var modelError in modelState.Errors)
                {
                    var errorText = ValidationHelpers.GetUserErrorMessageOrDefault(modelError, modelState: null);

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

            var tagBuilder = new TagBuilder("div");
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));

            if (viewContext.ViewData.ModelState.IsValid)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationSummaryValidCssClassName);
            }
            else
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationSummaryCssClassName);
            }

            tagBuilder.InnerHtml = wrappedMessage + unorderedList.ToString(TagRenderMode.Normal);

            if (formContext != null && !excludePropertyErrors)
            {
                // Inform the client where to replace the list of property errors after validation.
                tagBuilder.MergeAttribute("data-valmsg-summary", "true");
            }

            return tagBuilder;
        }

        /// <inheritdoc />
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string name)
        {
            var actionBindingContext = _actionBindingContextProvider.GetActionBindingContextAsync(viewContext).Result;
            metadata = metadata ??
                ExpressionMetadataProvider.FromStringExpression(name, viewContext.ViewData, _metadataProvider);

            return actionBindingContext
                .ValidatorProvider
                .GetValidators(metadata)
                .OfType<IClientModelValidator>()
                .SelectMany(v => v.GetClientValidationRules(
                    new ClientModelValidationContext(metadata, _metadataProvider)));
        }

        internal static string EvalString(ViewContext viewContext, string key, string format)
        {
            return Convert.ToString(viewContext.ViewData.Eval(key, format), CultureInfo.CurrentCulture);
        }

        /// <remarks>
        /// Not used directly in HtmlHelper. Exposed for use in DefaultDisplayTemplates.
        /// </remarks>
        internal static TagBuilder GenerateOption(SelectListItem item, string encodedText)
        {
            var tagBuilder = new TagBuilder("option")
            {
                InnerHtml = encodedText,
            };

            if (item.Value != null)
            {
                tagBuilder.Attributes["value"] = item.Value;
            }

            if (item.Selected)
            {
                tagBuilder.Attributes["selected"] = "selected";
            }

            if (item.Disabled)
            {
                tagBuilder.Attributes["disabled"] = "disabled";
            }

            return tagBuilder;
        }

        internal static string GetFullHtmlFieldName(ViewContext viewContext, string name)
        {
            var fullName = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return fullName;
        }

        internal static object GetModelStateValue(ViewContext viewContext, string key, Type destinationType)
        {
            ModelState modelState;
            if (viewContext.ViewData.ModelState.TryGetValue(key, out modelState) && modelState.Value != null)
            {
                return modelState.Value.ConvertTo(destinationType, culture: null);
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
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
        /// </returns>
        protected virtual TagBuilder GenerateFormCore(
            [NotNull] ViewContext viewContext,
            string action,
            string method,
            object htmlAttributes)
        {
            var tagBuilder = new TagBuilder("form");
            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));

            // action is implicitly generated from other parameters, so htmlAttributes take precedence.
            tagBuilder.MergeAttribute("action", action);

            if (string.IsNullOrEmpty(method))
            {
                // Occurs only when called from a tag helper.
                method = FormMethod.Post.ToString().ToLowerInvariant();
            }

            // For tag helpers, htmlAttributes will be null; replaceExisting value does not matter.
            // method is an explicit parameter to HTML helpers, so it takes precedence over the htmlAttributes.
            tagBuilder.MergeAttribute("method", method, replaceExisting: true);

            return tagBuilder;
        }

        protected virtual TagBuilder GenerateInput(
            [NotNull] ViewContext viewContext,
            InputType inputType,
            ModelMetadata metadata,
            string name,
            object value,
            bool useViewData,
            bool isChecked,
            bool setId,
            bool isExplicitValue,
            string format,
            IDictionary<string, object> htmlAttributes)
        {
            // Not valid to use TextBoxForModel() and so on in a top-level view; would end up with an unnamed input
            // elements. But we support the *ForModel() methods in any lower-level template, once HtmlFieldPrefix is
            // non-empty.
            var fullName = GetFullHtmlFieldName(viewContext, name);
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
                        isChecked = EvalBoolean(viewContext, fullName);
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
                        attributeValue = useViewData ? EvalString(viewContext, fullName, format) : valueParameter;
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
            if (viewContext.ViewData.ModelState.TryGetValue(fullName, out modelState) && modelState.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }

            tagBuilder.MergeAttributes(GetValidationAttributes(viewContext, metadata, name));

            return tagBuilder;
        }

        protected virtual TagBuilder GenerateLink(
            [NotNull] string linkText,
            [NotNull] string url,
            object htmlAttributes)
        {
            var tagBuilder = new TagBuilder("a")
            {
                InnerHtml = WebUtility.HtmlEncode(linkText),
            };

            tagBuilder.MergeAttributes(GetHtmlAttributeDictionaryOrNull(htmlAttributes));
            tagBuilder.MergeAttribute("href", url);

            return tagBuilder;
        }

        // Only render attributes if client-side validation is enabled, and then only if we've
        // never rendered validation for a field with this name in this form.
        protected virtual IDictionary<string, object> GetValidationAttributes(
            ViewContext viewContext,
            ModelMetadata metadata,
            string name)
        {
            var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
            if (formContext == null)
            {
                return null;
            }

            var fullName = GetFullHtmlFieldName(viewContext, name);
            if (formContext.RenderedField(fullName))
            {
                return null;
            }

            formContext.RenderedField(fullName, true);
            var clientRules = GetClientValidationRules(viewContext, metadata, name);

            return UnobtrusiveValidationAttributesGenerator.GetValidationAttributes(clientRules);
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

        private static IEnumerable<SelectListItem> GetSelectListItems([NotNull] ViewContext viewContext, string name)
        {
            var value = viewContext.ViewData.Eval(name);
            if (value == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_MissingSelectData(
                    "IEnumerable<SelectListItem>", name));
            }

            var selectList = value as IEnumerable<SelectListItem>;
            if (selectList == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_WrongSelectDataType(
                    name, value.GetType().FullName, "IEnumerable<SelectListItem>"));
            }

            return selectList;
        }

        private static IEnumerable<SelectListItem> UpdateSelectListItemsWithDefaultValue(
            IEnumerable<SelectListItem> selectList,
            object defaultValue,
            bool allowMultiple,
            out ICollection<string> selectedValues)
        {
            IEnumerable defaultValues;
            if (allowMultiple)
            {
                defaultValues = defaultValue as IEnumerable;
                if (defaultValues == null || defaultValues is string)
                {
                    throw new InvalidOperationException(
                        Resources.FormatHtmlHelper_SelectExpressionNotEnumerable("expression"));
                }
            }
            else
            {
                defaultValues = new[] { defaultValue };
            }

            var values =
                defaultValues.OfType<object>().Select(value => Convert.ToString(value, CultureInfo.CurrentCulture));

            // ToString() by default returns an enum value's name.  But selectList may use numeric values.
            var enumValues = defaultValues.OfType<Enum>().Select(value => value.ToString());
            values = values.Concat(enumValues);

            selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
            var newSelectList = new List<SelectListItem>();
            foreach (SelectListItem item in selectList)
            {
                item.Selected =
                    (item.Value != null) ? selectedValues.Contains(item.Value) : selectedValues.Contains(item.Text);
                newSelectList.Add(item);
            }

            return newSelectList;
        }

        private StringBuilder GenerateGroupsAndOptions(string optionLabel, IEnumerable<SelectListItem> selectList)
        {
            var listItemBuilder = new StringBuilder();

            // Make optionLabel the first item that gets rendered.
            if (optionLabel != null)
            {
                listItemBuilder.AppendLine(GenerateOption(new SelectListItem()
                {
                    Text = optionLabel,
                    Value = string.Empty,
                    Selected = false,
                }));
            }

            // Group items in the SelectList if requested.
            // Treat each item with Group == null as a member of a unique group
            // so they are added according to the original order.
            var groupedSelectList = selectList.GroupBy<SelectListItem, int>(
                item => (item.Group == null) ? item.GetHashCode() : item.Group.GetHashCode());
            foreach (var group in groupedSelectList)
            {
                var optGroup = group.First().Group;

                // Wrap if requested.
                TagBuilder groupBuilder = null;
                if (optGroup != null)
                {
                    groupBuilder = new TagBuilder("optgroup");
                    if (optGroup.Name != null)
                    {
                        groupBuilder.MergeAttribute("label", optGroup.Name);
                    }

                    if (optGroup.Disabled)
                    {
                        groupBuilder.MergeAttribute("disabled", "disabled");
                    }

                    listItemBuilder.AppendLine(groupBuilder.ToString(TagRenderMode.StartTag));
                }

                foreach (var item in group)
                {
                    listItemBuilder.AppendLine(GenerateOption(item));
                }

                if (optGroup != null)
                {
                    listItemBuilder.AppendLine(groupBuilder.ToString(TagRenderMode.EndTag));
                }
            }

            return listItemBuilder;
        }

        private string GenerateOption(SelectListItem item)
        {
            var encodedText = Encode(item.Text);
            var tagBuilder = GenerateOption(item, encodedText);

            return tagBuilder.ToString(TagRenderMode.Normal);
        }
    }
}