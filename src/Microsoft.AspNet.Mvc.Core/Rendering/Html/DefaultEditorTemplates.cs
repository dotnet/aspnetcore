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

        public static string BooleanTemplate(IHtmlHelper html)
        {
            bool? value = null;
            if (html.ViewData.Model != null)
            {
                value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
            }

            return html.ViewData.ModelMetadata.IsNullableValueType ?
                BooleanTemplateDropDownList(html, value) :
                BooleanTemplateCheckbox(html, value ?? false);
        }

        private static string BooleanTemplateCheckbox(IHtmlHelper html, bool value)
        {
            return html.CheckBox(string.Empty, value, CreateHtmlAttributes(html, "check-box")).ToString();
        }

        private static string BooleanTemplateDropDownList(IHtmlHelper html, bool? value)
        {
            return html.DropDownList(
                string.Empty,
                DefaultDisplayTemplates.TriStateValues(value),
                CreateHtmlAttributes(html, "list-box tri-state"))
                    .ToString();
        }

        public static string CollectionTemplate(IHtmlHelper html)
        {
            var viewData = html.ViewData;
            var model = viewData.ModelMetadata.Model;
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

                var serviceProvider = html.ViewContext.HttpContext.RequestServices;
                var metadataProvider = serviceProvider.GetService<IModelMetadataProvider>();
                var viewEngine = serviceProvider.GetService<ICompositeViewEngine>();

                var index = 0;
                foreach (var item in collection)
                {
                    var itemType = typeInCollection;
                    if (item != null && !typeInCollectionIsNullableValueType)
                    {
                        itemType = item.GetType();
                    }

                    var metadata = metadataProvider.GetMetadataForType(() => item, itemType);
                    var fieldName = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);

                    var templateBuilder = new TemplateBuilder(
                        viewEngine,
                        html.ViewContext,
                        html.ViewData,
                        metadata,
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

        public static string DecimalTemplate(IHtmlHelper html)
        {
            if (html.ViewData.TemplateInfo.FormattedModelValue == html.ViewData.ModelMetadata.Model)
            {
                html.ViewData.TemplateInfo.FormattedModelValue =
                    string.Format(CultureInfo.CurrentCulture, "{0:0.00}", html.ViewData.ModelMetadata.Model);
            }

            return StringTemplate(html);
        }

        public static string HiddenInputTemplate(IHtmlHelper html)
        {
            var viewData = html.ViewData;

            // TODO: add ModelMetadata.HideSurroundingHtml and use here (set result to string.Empty)
            var result = DefaultDisplayTemplates.StringTemplate(html);

            var model = viewData.Model;

            // Special-case opaque values and arbitrary binary data.
            var modelAsByteArray = model as byte[];
            if (modelAsByteArray != null)
            {
                model = Convert.ToBase64String(modelAsByteArray);
            }

            var htmlAttributesObject = viewData[HtmlAttributeKey];
            var hiddenResult = html.Hidden(string.Empty, model, htmlAttributesObject);
            result += hiddenResult.ToString();

            return result;
        }

        private static IDictionary<string, object> CreateHtmlAttributes(
            IHtmlHelper html,
            string className,
            string inputType = null)
        {
            var htmlAttributesObject = html.ViewData[HtmlAttributeKey];
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

        public static string ObjectTemplate(IHtmlHelper html)
        {
            var viewData = html.ViewData;
            var templateInfo = viewData.TemplateInfo;
            var modelMetadata = viewData.ModelMetadata;
            var builder = new StringBuilder();

            if (templateInfo.TemplateDepth > 1)
            {
                return modelMetadata.Model == null ? modelMetadata.NullDisplayText : modelMetadata.SimpleDisplayText;
            }

            var serviceProvider = html.ViewContext.HttpContext.RequestServices;
            var viewEngine = serviceProvider.GetService<ICompositeViewEngine>();

            foreach (var propertyMetadata in modelMetadata.Properties.Where(pm => ShouldShow(pm, templateInfo)))
            {
                var divTag = new TagBuilder("div");

                // TODO: add ModelMetadata.HideSurroundingHtml and use here (skip this block)
                {
                    var label = html.Label(propertyMetadata.PropertyName).ToString();
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
                    html.ViewContext,
                    html.ViewData,
                    propertyMetadata,
                    htmlFieldName: propertyMetadata.PropertyName,
                    templateName: null,
                    readOnly: false,
                    additionalViewData: null);

                builder.Append(templateBuilder.Build());

                // TODO: add ModelMetadata.HideSurroundingHtml and use here (skip this block)
                // TODO: Add IHtmlHelper.ValidationMessage() and call just prior to closing the <div/> tag
                {
                    builder.Append(" ");
                    builder.AppendLine(divTag.ToString(TagRenderMode.EndTag));
                }
            }

            return builder.ToString();
        }

        public static string PasswordTemplate(IHtmlHelper html)
        {
            return html.Password(string.Empty,
                html.ViewData.TemplateInfo.FormattedModelValue,
                CreateHtmlAttributes(html, "text-box single-line password"))
                    .ToString();
        }

        private static bool ShouldShow(ModelMetadata metadata, TemplateInfo templateInfo)
        {
            return
                metadata.ShowForEdit &&
                !metadata.IsComplexType
                && !templateInfo.Visited(metadata);
        }

        public static string StringTemplate(IHtmlHelper html)
        {
            return GenerateTextBox(html);
        }

        public static string PhoneNumberInputTemplate(IHtmlHelper html)
        {
            return GenerateTextBox(html, inputType: "tel");
        }

        public static string UrlInputTemplate(IHtmlHelper html)
        {
            return GenerateTextBox(html, inputType: "url");
        }

        public static string EmailAddressInputTemplate(IHtmlHelper html)
        {
            return GenerateTextBox(html, inputType: "email");
        }

        public static string DateTimeInputTemplate(IHtmlHelper html)
        {
            ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fffK}");
            return GenerateTextBox(html, inputType: "datetime");
        }

        public static string DateTimeLocalInputTemplate(IHtmlHelper html)
        {
            ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fff}");
            return GenerateTextBox(html, inputType: "datetime-local");
        }

        public static string DateInputTemplate(IHtmlHelper html)
        {
            ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-dd}");
            return GenerateTextBox(html, inputType: "date");
        }

        public static string TimeInputTemplate(IHtmlHelper html)
        {
            ApplyRfc3339DateFormattingIfNeeded(html, "{0:HH:mm:ss.fff}");
            return GenerateTextBox(html, inputType: "time");
        }

        public static string NumberInputTemplate(IHtmlHelper html)
        {
            return GenerateTextBox(html, inputType: "number");
        }

        private static void ApplyRfc3339DateFormattingIfNeeded(IHtmlHelper html, string format)
        {
            if (html.Html5DateRenderingMode != Html5DateRenderingMode.Rfc3339)
            {
                return;
            }

            var metadata = html.ViewData.ModelMetadata;
            var value = metadata.Model;

            // TODO: add ModelMetadata.HasNonDefaultEditFormat and use here (also return if true)
            if (html.ViewData.TemplateInfo.FormattedModelValue != value)
            {
                return;
            }

            if (value is DateTime || value is DateTimeOffset)
            {
                html.ViewData.TemplateInfo.FormattedModelValue =
                    string.Format(CultureInfo.InvariantCulture, format, value);
            }
        }

        private static string GenerateTextBox(IHtmlHelper html, string inputType = null)
        {
            return GenerateTextBox(html, inputType, html.ViewData.TemplateInfo.FormattedModelValue);
        }

        private static string GenerateTextBox(IHtmlHelper html, string inputType, object value)
        {
            return html.TextBox(
                name: string.Empty,
                value: value,
                htmlAttributes: CreateHtmlAttributes(html, className: "text-box single-line", inputType: inputType))
                    .ToString();
        }
    }
}