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
    public static class DefaultDisplayTemplates
    {
        public static string BooleanTemplate(IHtmlHelper html)
        {
            bool? value = null;
            if (html.ViewData.Model != null)
            {
                value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
            }

            return html.ViewData.ModelMetadata.IsNullableValueType ?
                BooleanTemplateDropDownList(html, value) :
                BooleanTemplateCheckbox(value ?? false);
        }

        private static string BooleanTemplateCheckbox(bool value)
        {
            var inputTag = new TagBuilder("input");
            inputTag.AddCssClass("check-box");
            inputTag.Attributes["disabled"] = "disabled";
            inputTag.Attributes["type"] = "checkbox";
            if (value)
            {
                inputTag.Attributes["checked"] = "checked";
            }

            return inputTag.ToString(TagRenderMode.SelfClosing);
        }

        private static string BooleanTemplateDropDownList(IHtmlHelper html, bool? value)
        {
            var selectTag = new TagBuilder("select");
            selectTag.AddCssClass("list-box");
            selectTag.AddCssClass("tri-state");
            selectTag.Attributes["disabled"] = "disabled";

            var builder = new StringBuilder();
            builder.Append(selectTag.ToString(TagRenderMode.StartTag));

            foreach (var item in TriStateValues(value))
            {
                var encodedText = html.Encode(item.Text);
                var option = DefaultHtmlGenerator.GenerateOption(item, encodedText);
                builder.Append(option);
            }

            builder.Append(selectTag.ToString(TagRenderMode.EndTag));
            return builder.ToString();
        }

        // Will soon need to be shared with the default editor templates implementations.
        internal static List<SelectListItem> TriStateValues(bool? value)
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = Resources.Common_TriState_NotSet,
                    Value = string.Empty,
                    Selected = !value.HasValue
                },
                new SelectListItem
                {
                    Text = Resources.Common_TriState_True,
                    Value = "true",
                    Selected = (value == true),
                },
                new SelectListItem
                {
                    Text = Resources.Common_TriState_False,
                    Value = "false",
                    Selected = (value == false),
                },
            };
        }

        public static string CollectionTemplate(IHtmlHelper html)
        {
            var model = html.ViewData.ModelMetadata.Model;
            if (model == null)
            {
                return string.Empty;
            }

            var collection = model as IEnumerable;
            if (collection == null)
            {
                // Only way we could reach here is if user passed templateName: "Collection" to a Display() overload.
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

            var oldPrefix = html.ViewData.TemplateInfo.HtmlFieldPrefix;

            try
            {
                html.ViewData.TemplateInfo.HtmlFieldPrefix = string.Empty;

                var fieldNameBase = oldPrefix;
                var result = new StringBuilder();

                var serviceProvider = html.ViewContext.HttpContext.RequestServices;
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

                    var metadata = metadataProvider.GetMetadataForType(() => item, itemType);
                    var fieldName = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);

                    var templateBuilder = new TemplateBuilder(
                        viewEngine,
                        html.ViewContext,
                        html.ViewData,
                        metadata,
                        htmlFieldName: fieldName,
                        templateName: null,
                        readOnly: true,
                        additionalViewData: null);

                    var output = templateBuilder.Build();
                    result.Append(output);
                }

                return result.ToString();
            }
            finally
            {
                html.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
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

        public static string EmailAddressTemplate(IHtmlHelper html)
        {
            var uriString = "mailto:" + ((html.ViewData.Model == null) ?
                string.Empty :
                html.ViewData.Model.ToString());
            var linkedText = (html.ViewData.TemplateInfo.FormattedModelValue == null) ?
                string.Empty :
                html.ViewData.TemplateInfo.FormattedModelValue.ToString();

            return HyperlinkTemplate(uriString, linkedText);
        }

        public static string HiddenInputTemplate(IHtmlHelper html)
        {
            if (html.ViewData.ModelMetadata.HideSurroundingHtml)
            {
                return string.Empty;
            }

            return StringTemplate(html);
        }

        public static string HtmlTemplate(IHtmlHelper html)
        {
            return html.ViewData.TemplateInfo.FormattedModelValue.ToString();
        }

        public static string ObjectTemplate(IHtmlHelper html)
        {
            var viewData = html.ViewData;
            var templateInfo = viewData.TemplateInfo;
            var modelMetadata = viewData.ModelMetadata;
            var builder = new StringBuilder();

            if (modelMetadata.Model == null)
            {
                return modelMetadata.NullDisplayText;
            }

            if (templateInfo.TemplateDepth > 1)
            {
                return modelMetadata.Model == null ? modelMetadata.NullDisplayText : modelMetadata.SimpleDisplayText;
            }

            var serviceProvider = html.ViewContext.HttpContext.RequestServices;
            var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();

            foreach (var propertyMetadata in modelMetadata.Properties.Where(pm => ShouldShow(pm, templateInfo)))
            {
                var divTag = new TagBuilder("div");

                if (!propertyMetadata.HideSurroundingHtml)
                {
                    var label = propertyMetadata.GetDisplayName();
                    if (!string.IsNullOrEmpty(label))
                    {
                        divTag.SetInnerText(label);
                        divTag.AddCssClass("display-label");
                        builder.AppendLine(divTag.ToString(TagRenderMode.Normal));

                        // Reset divTag for reuse.
                        divTag.Attributes.Clear();
                    }

                    divTag.AddCssClass("display-field");
                    builder.Append(divTag.ToString(TagRenderMode.StartTag));
                }

                var templateBuilder = new TemplateBuilder(
                    viewEngine,
                    html.ViewContext,
                    html.ViewData,
                    propertyMetadata,
                    htmlFieldName: propertyMetadata.PropertyName,
                    templateName: null,
                    readOnly: true,
                    additionalViewData: null);

                builder.Append(templateBuilder.Build());

                if (!propertyMetadata.HideSurroundingHtml)
                {
                    builder.AppendLine(divTag.ToString(TagRenderMode.EndTag));
                }
            }

            return builder.ToString();
        }

        private static bool ShouldShow(ModelMetadata metadata, TemplateInfo templateInfo)
        {
            return
                metadata.ShowForDisplay &&
                !metadata.IsComplexType &&
                !templateInfo.Visited(metadata);
        }

        public static string StringTemplate(IHtmlHelper html)
        {
            return html.Encode(html.ViewData.TemplateInfo.FormattedModelValue);
        }

        public static string UrlTemplate(IHtmlHelper html)
        {
            var uriString = (html.ViewData.Model == null) ? string.Empty : html.ViewData.Model.ToString();
            var linkedText = (html.ViewData.TemplateInfo.FormattedModelValue == null) ?
                string.Empty :
                html.ViewData.TemplateInfo.FormattedModelValue.ToString();

            return HyperlinkTemplate(uriString, linkedText);
        }

        // Neither uriString nor linkedText need be encoded prior to calling this method.
        private static string HyperlinkTemplate(string uriString, string linkedText)
        {
            var hyperlinkTag = new TagBuilder("a");
            hyperlinkTag.MergeAttribute("href", uriString);
            hyperlinkTag.SetInnerText(linkedText);

            return hyperlinkTag.ToString(TagRenderMode.Normal);
        }
    }
}
