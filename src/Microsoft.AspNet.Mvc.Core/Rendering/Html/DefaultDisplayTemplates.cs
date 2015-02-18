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
        public static string BooleanTemplate(IHtmlHelper htmlHelper)
        {
            bool? value = null;
            if (htmlHelper.ViewData.Model != null)
            {
                value = Convert.ToBoolean(htmlHelper.ViewData.Model, CultureInfo.InvariantCulture);
            }

            return htmlHelper.ViewData.ModelMetadata.IsNullableValueType ?
                BooleanTemplateDropDownList(htmlHelper, value) :
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

        private static string BooleanTemplateDropDownList(IHtmlHelper htmlHelper, bool? value)
        {
            var selectTag = new TagBuilder("select");
            selectTag.AddCssClass("list-box");
            selectTag.AddCssClass("tri-state");
            selectTag.Attributes["disabled"] = "disabled";

            var builder = new StringBuilder();
            builder.Append(selectTag.ToString(TagRenderMode.StartTag));

            foreach (var item in TriStateValues(value))
            {
                var encodedText = htmlHelper.Encode(item.Text);
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

        public static string CollectionTemplate(IHtmlHelper htmlHelper)
        {
            var model = htmlHelper.ViewData.Model;
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

            var oldPrefix = htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix;

            try
            {
                htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix = string.Empty;

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
                        readOnly: true,
                        additionalViewData: null);

                    var output = templateBuilder.Build();
                    result.Append(output);
                }

                return result.ToString();
            }
            finally
            {
                htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
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

        public static string EmailAddressTemplate(IHtmlHelper htmlHelper)
        {
            var uriString = "mailto:" + ((htmlHelper.ViewData.Model == null) ?
                string.Empty :
                htmlHelper.ViewData.Model.ToString());
            var linkedText = (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == null) ?
                string.Empty :
                htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString();

            return HyperlinkTemplate(uriString, linkedText);
        }

        public static string HiddenInputTemplate(IHtmlHelper htmlHelper)
        {
            if (htmlHelper.ViewData.ModelMetadata.HideSurroundingHtml)
            {
                return string.Empty;
            }

            return StringTemplate(htmlHelper);
        }

        public static string HtmlTemplate(IHtmlHelper htmlHelper)
        {
            return htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString();
        }

        public static string ObjectTemplate(IHtmlHelper htmlHelper)
        {
            var viewData = htmlHelper.ViewData;
            var templateInfo = viewData.TemplateInfo;
            var modelExplorer = viewData.ModelExplorer;
            var builder = new StringBuilder();

            if (modelExplorer.Model == null)
            {
                return modelExplorer.Metadata.NullDisplayText;
            }

            if (templateInfo.TemplateDepth > 1)
            {
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
                    htmlHelper.ViewContext,
                    htmlHelper.ViewData,
                    propertyExplorer,
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

        private static bool ShouldShow(ModelExplorer modelExplorer, TemplateInfo templateInfo)
        {
            return
                modelExplorer.Metadata.ShowForDisplay &&
                !modelExplorer.Metadata.IsComplexType &&
                !templateInfo.Visited(modelExplorer);
        }

        public static string StringTemplate(IHtmlHelper htmlHelper)
        {
            return htmlHelper.Encode(htmlHelper.ViewData.TemplateInfo.FormattedModelValue);
        }

        public static string UrlTemplate(IHtmlHelper htmlHelper)
        {
            var uriString = (htmlHelper.ViewData.Model == null) ? string.Empty : htmlHelper.ViewData.Model.ToString();
            var linkedText = (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == null) ?
                string.Empty :
                htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString();

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
