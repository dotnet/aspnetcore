// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class DefaultEditorTemplates
    {
        private const string HtmlAttributeKey = "htmlAttributes";

        public static string BooleanTemplate(IHtmlHelper htmlHelper)
        {
            bool? value = null;
            if (htmlHelper.ViewData.Model != null)
            {
                value = Convert.ToBoolean(htmlHelper.ViewData.Model, CultureInfo.InvariantCulture);
            }

            return htmlHelper.ViewData.ModelMetadata.IsNullableValueType ?
                BooleanTemplateDropDownList(htmlHelper, value) :
                BooleanTemplateCheckbox(htmlHelper, value ?? false);
        }

        private static string BooleanTemplateCheckbox(IHtmlHelper htmlHelper, bool value)
        {
            return htmlHelper.CheckBox(
                expression: null,
                isChecked: value,
                htmlAttributes: CreateHtmlAttributes(htmlHelper, "check-box"))
                    .ToString();
        }

        private static string BooleanTemplateDropDownList(IHtmlHelper htmlHelper, bool? value)
        {
            return htmlHelper.DropDownList(
                expression: null,
                selectList: DefaultDisplayTemplates.TriStateValues(value),
                optionLabel: null,
                htmlAttributes: CreateHtmlAttributes(htmlHelper, "list-box tri-state"))
                    .ToString();
        }

        public static string CollectionTemplate(IHtmlHelper htmlHelper)
        {
            var viewData = htmlHelper.ViewData;
            var model = viewData.Model;
            if (model == null)
            {
                return string.Empty;
            }

            var collection = model as IEnumerable;
            if (collection == null)
            {
                // Only way we could reach here is if user passed templateName: "Collection" to an Editor() overload.
                throw new InvalidOperationException(Resources.FormatTemplates_TypeMustImplementIEnumerable(
                    "Collection", model.GetType().FullName, typeof(IEnumerable).FullName));
            }

            var typeInCollection = typeof(string);
            var genericEnumerableType = collection.GetType().ExtractGenericInterface(typeof(IEnumerable<>));
            if (genericEnumerableType != null)
            {
                typeInCollection = genericEnumerableType.GetGenericArguments()[0];
            }

            var typeInCollectionIsNullableValueType = typeInCollection.IsNullableValueType();
            var oldPrefix = viewData.TemplateInfo.HtmlFieldPrefix;

            try
            {
                viewData.TemplateInfo.HtmlFieldPrefix = string.Empty;

                var fieldNameBase = oldPrefix;
                var result = new StringBuilder();

                var serviceProvider = htmlHelper.ViewContext.HttpContext.RequestServices;
                var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
                var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();

                var index = 0;
                foreach (var item in collection)
                {
                    var itemType = typeInCollection;
                    if (item != null && !typeInCollectionIsNullableValueType)
                    {
                        itemType = item.GetType();
                    }

                    var modelExplorer = metadataProvider.GetModelExplorerForType(itemType, item);
                    var fieldName = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);

                    var templateBuilder = new TemplateBuilder(
                        viewEngine,
                        htmlHelper.ViewContext,
                        htmlHelper.ViewData,
                        modelExplorer,
                        htmlFieldName: fieldName,
                        templateName: null,
                        readOnly: false,
                        additionalViewData: null);

                    var output = templateBuilder.Build();
                    result.Append(output);
                }

                return result.ToString();
            }
            finally
            {
                viewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
            }
        }

        public static string DecimalTemplate(IHtmlHelper htmlHelper)
        {
            if (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == htmlHelper.ViewData.Model)
            {
                htmlHelper.ViewData.TemplateInfo.FormattedModelValue =
                    string.Format(CultureInfo.CurrentCulture, "{0:0.00}", htmlHelper.ViewData.Model);
            }

            return StringTemplate(htmlHelper);
        }

        public static string HiddenInputTemplate(IHtmlHelper htmlHelper)
        {
            var viewData = htmlHelper.ViewData;
            var model = viewData.Model;

            string result;
            if (viewData.ModelMetadata.HideSurroundingHtml)
            {
                result = string.Empty;
            }
            else
            {
                result = DefaultDisplayTemplates.StringTemplate(htmlHelper);
            }

            // Special-case opaque values and arbitrary binary data.
            var modelAsByteArray = model as byte[];
            if (modelAsByteArray != null)
            {
                model = Convert.ToBase64String(modelAsByteArray);
            }

            var htmlAttributesObject = viewData[HtmlAttributeKey];
            var hiddenResult = htmlHelper.Hidden(expression: null, value: model, htmlAttributes: htmlAttributesObject);
            result += hiddenResult.ToString();

            return result;
        }

        private static IDictionary<string, object> CreateHtmlAttributes(
            IHtmlHelper htmlHelper,
            string className,
            string inputType = null)
        {
            var htmlAttributesObject = htmlHelper.ViewData[HtmlAttributeKey];
            if (htmlAttributesObject != null)
            {
                return MergeHtmlAttributes(htmlAttributesObject, className, inputType);
            }

            var htmlAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "class", className }
            };

            if (inputType != null)
            {
                htmlAttributes.Add("type", inputType);
            }

            return htmlAttributes;
        }

        private static IDictionary<string, object> MergeHtmlAttributes(
            object htmlAttributesObject,
            string className,
            string inputType)
        {
            var htmlAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributesObject);

            object htmlClassObject;
            if (htmlAttributes.TryGetValue("class", out htmlClassObject))
            {
                var htmlClassName = htmlClassObject.ToString() + " " + className;
                htmlAttributes["class"] = htmlClassName;
            }
            else
            {
                htmlAttributes.Add("class", className);
            }

            // The input type from the provided htmlAttributes overrides the inputType parameter.
            if (inputType != null && !htmlAttributes.ContainsKey("type"))
            {
                htmlAttributes.Add("type", inputType);
            }

            return htmlAttributes;
        }

        public static string MultilineTemplate(IHtmlHelper htmlHelper)
        {
            var htmlString = htmlHelper.TextArea(
                expression: string.Empty,
                value: htmlHelper.ViewContext.ViewData.TemplateInfo.FormattedModelValue.ToString(),
                rows: 0,
                columns: 0,
                htmlAttributes: CreateHtmlAttributes(htmlHelper, "text-box multi-line"));
            return htmlString.ToString();
        }

        public static string ObjectTemplate(IHtmlHelper htmlHelper)
        {
            var viewData = htmlHelper.ViewData;
            var templateInfo = viewData.TemplateInfo;
            var modelExplorer = viewData.ModelExplorer;
            var builder = new StringBuilder();

            if (templateInfo.TemplateDepth > 1)
            {
                if (modelExplorer.Model == null)
                {
                    return modelExplorer.Metadata.NullDisplayText;
                }

                var text = modelExplorer.GetSimpleDisplayText();
                if (modelExplorer.Metadata.HtmlEncode)
                {
                    text = htmlHelper.Encode(text);
                }

                return text;
            }

            var serviceProvider = htmlHelper.ViewContext.HttpContext.RequestServices;
            var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();

            foreach (var propertyExplorer in modelExplorer.Properties)
            {
                var propertyMetadata = propertyExplorer.Metadata;
                if (!ShouldShow(propertyExplorer, templateInfo))
                {
                    continue;
                }

                var divTag = new TagBuilder("div", htmlHelper.HtmlEncoder);

                if (!propertyMetadata.HideSurroundingHtml)
                {
                    var label = htmlHelper.Label(
                        propertyMetadata.PropertyName,
                        labelText: null,
                        htmlAttributes: null)
                            .ToString();
                    if (!string.IsNullOrEmpty(label))
                    {
                        divTag.AddCssClass("editor-label");
                        divTag.InnerHtml = label; // already escaped
                        builder.AppendLine(divTag.ToString(TagRenderMode.Normal));

                        // Reset divTag for reuse.
                        divTag.Attributes.Clear();
                    }

                    divTag.AddCssClass("editor-field");
                    builder.Append(divTag.ToString(TagRenderMode.StartTag));
                }

                var templateBuilder = new TemplateBuilder(
                    viewEngine,
                    htmlHelper.ViewContext,
                    htmlHelper.ViewData,
                    propertyExplorer,
                    htmlFieldName: propertyMetadata.PropertyName,
                    templateName: null,
                    readOnly: false,
                    additionalViewData: null);

                builder.Append(templateBuilder.Build());

                if (!propertyMetadata.HideSurroundingHtml)
                {
                    builder.Append(" ");
                    builder.Append(htmlHelper.ValidationMessage(
                        propertyMetadata.PropertyName,
                        message: null,
                        htmlAttributes: null,
                        tag: null));

                    builder.AppendLine(divTag.ToString(TagRenderMode.EndTag));
                }
            }

            return builder.ToString();
        }

        public static string PasswordTemplate(IHtmlHelper htmlHelper)
        {
            return htmlHelper.Password(
                expression: null,
                value: htmlHelper.ViewData.TemplateInfo.FormattedModelValue,
                htmlAttributes: CreateHtmlAttributes(htmlHelper, "text-box single-line password"))
                    .ToString();
        }

        private static bool ShouldShow(ModelExplorer modelExplorer, TemplateInfo templateInfo)
        {
            return
                modelExplorer.Metadata.ShowForEdit &&
                !modelExplorer.Metadata.IsComplexType &&
                !templateInfo.Visited(modelExplorer);
        }

        public static string StringTemplate(IHtmlHelper htmlHelper)
        {
            return GenerateTextBox(htmlHelper);
        }

        public static string PhoneNumberInputTemplate(IHtmlHelper htmlHelper)
        {
            return GenerateTextBox(htmlHelper, inputType: "tel");
        }

        public static string UrlInputTemplate(IHtmlHelper htmlHelper)
        {
            return GenerateTextBox(htmlHelper, inputType: "url");
        }

        public static string EmailAddressInputTemplate(IHtmlHelper htmlHelper)
        {
            return GenerateTextBox(htmlHelper, inputType: "email");
        }

        public static string DateTimeInputTemplate(IHtmlHelper htmlHelper)
        {
            ApplyRfc3339DateFormattingIfNeeded(htmlHelper, "{0:yyyy-MM-ddTHH:mm:ss.fffK}");
            return GenerateTextBox(htmlHelper, inputType: "datetime");
        }

        public static string DateTimeLocalInputTemplate(IHtmlHelper htmlHelper)
        {
            ApplyRfc3339DateFormattingIfNeeded(htmlHelper, "{0:yyyy-MM-ddTHH:mm:ss.fff}");
            return GenerateTextBox(htmlHelper, inputType: "datetime-local");
        }

        public static string DateInputTemplate(IHtmlHelper htmlHelper)
        {
            ApplyRfc3339DateFormattingIfNeeded(htmlHelper, "{0:yyyy-MM-dd}");
            return GenerateTextBox(htmlHelper, inputType: "date");
        }

        public static string TimeInputTemplate(IHtmlHelper htmlHelper)
        {
            ApplyRfc3339DateFormattingIfNeeded(htmlHelper, "{0:HH:mm:ss.fff}");
            return GenerateTextBox(htmlHelper, inputType: "time");
        }

        public static string NumberInputTemplate(IHtmlHelper htmlHelper)
        {
            return GenerateTextBox(htmlHelper, inputType: "number");
        }

        private static void ApplyRfc3339DateFormattingIfNeeded(IHtmlHelper htmlHelper, string format)
        {
            if (htmlHelper.Html5DateRenderingMode != Html5DateRenderingMode.Rfc3339)
            {
                return;
            }

            var metadata = htmlHelper.ViewData.ModelMetadata;
            var value = htmlHelper.ViewData.Model;
            if (htmlHelper.ViewData.TemplateInfo.FormattedModelValue != value && metadata.HasNonDefaultEditFormat)
            {
                return;
            }

            if (value is DateTime || value is DateTimeOffset)
            {
                htmlHelper.ViewData.TemplateInfo.FormattedModelValue =
                    string.Format(CultureInfo.InvariantCulture, format, value);
            }
        }

        private static string GenerateTextBox(IHtmlHelper htmlHelper, string inputType = null)
        {
            return GenerateTextBox(htmlHelper, inputType, htmlHelper.ViewData.TemplateInfo.FormattedModelValue);
        }

        private static string GenerateTextBox(IHtmlHelper htmlHelper, string inputType, object value)
        {
            var htmlAttributes =
                CreateHtmlAttributes(htmlHelper, className: "text-box single-line", inputType: inputType);

            return htmlHelper.TextBox(
                current: null,
                value: value,
                format: null,
                htmlAttributes: htmlAttributes)
                    .ToString();
        }
    }
}
