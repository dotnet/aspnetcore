// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class DefaultDisplayTemplates
{
    public static IHtmlContent BooleanTemplate(IHtmlHelper htmlHelper)
    {
        bool? value = null;
        if (htmlHelper.ViewData.Model != null)
        {
            value = Convert.ToBoolean(htmlHelper.ViewData.Model, CultureInfo.InvariantCulture);
        }

        return htmlHelper.ViewData.ModelMetadata.IsNullableValueType ?
            BooleanTemplateDropDownList(value) :
            BooleanTemplateCheckbox(value ?? false);
    }

    private static IHtmlContent BooleanTemplateCheckbox(bool value)
    {
        var inputTag = new TagBuilder("input");
        inputTag.AddCssClass("check-box");
        inputTag.Attributes["disabled"] = "disabled";
        inputTag.Attributes["type"] = "checkbox";
        if (value)
        {
            inputTag.Attributes["checked"] = "checked";
        }

        inputTag.TagRenderMode = TagRenderMode.SelfClosing;
        return inputTag;
    }

    private static IHtmlContent BooleanTemplateDropDownList(bool? value)
    {
        var selectTag = new TagBuilder("select");
        selectTag.AddCssClass("list-box");
        selectTag.AddCssClass("tri-state");
        selectTag.Attributes["disabled"] = "disabled";

        foreach (var item in TriStateValues(value))
        {
            selectTag.InnerHtml.AppendHtml(DefaultHtmlGenerator.GenerateOption(item, item.Text));
        }

        return selectTag;
    }

    // Will soon need to be shared with the default editor templates implementations.
    internal static List<SelectListItem> TriStateValues(bool? value)
    {
        return new List<SelectListItem>
            {
                new SelectListItem(Resources.Common_TriState_NotSet, string.Empty, !value.HasValue),
                new SelectListItem(Resources.Common_TriState_True, "true", (value == true)),
                new SelectListItem(Resources.Common_TriState_False, "false", (value == false)),
            };
    }

    public static IHtmlContent CollectionTemplate(IHtmlHelper htmlHelper)
    {
        var model = htmlHelper.ViewData.Model;
        if (model == null)
        {
            return HtmlString.Empty;
        }

        var enumerable = model as IEnumerable;
        if (enumerable == null)
        {
            // Only way we could reach here is if user passed templateName: "Collection" to a Display() overload.
            throw new InvalidOperationException(Resources.FormatTemplates_TypeMustImplementIEnumerable(
                "Collection", model.GetType().FullName, typeof(IEnumerable).FullName));
        }

        var elementMetadata = htmlHelper.ViewData.ModelMetadata.ElementMetadata;
        Debug.Assert(elementMetadata != null);
        var typeInCollectionIsNullableValueType = elementMetadata.IsNullableValueType;

        var serviceProvider = htmlHelper.ViewContext.HttpContext.RequestServices;
        var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();

        // Use typeof(string) instead of typeof(object) for IEnumerable collections. Neither type is Nullable<T>.
        if (elementMetadata.ModelType == typeof(object))
        {
            elementMetadata = metadataProvider.GetMetadataForType(typeof(string));
        }

        var oldPrefix = htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix;
        try
        {
            htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix = string.Empty;

            var collection = model as ICollection;
            var result = collection == null ? new HtmlContentBuilder() : new HtmlContentBuilder(collection.Count);
            var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();
            var viewBufferScope = serviceProvider.GetRequiredService<IViewBufferScope>();

            var index = 0;
            foreach (var item in enumerable)
            {
                var itemMetadata = elementMetadata;
                if (item != null && !typeInCollectionIsNullableValueType)
                {
                    itemMetadata = metadataProvider.GetMetadataForType(item.GetType());
                }

                var modelExplorer = new ModelExplorer(
                    metadataProvider,
                    container: htmlHelper.ViewData.ModelExplorer,
                    metadata: itemMetadata,
                    model: item);
                var fieldName = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", oldPrefix, index++);

                var templateBuilder = new TemplateBuilder(
                    viewEngine,
                    viewBufferScope,
                    htmlHelper.ViewContext,
                    htmlHelper.ViewData,
                    modelExplorer,
                    htmlFieldName: fieldName,
                    templateName: null,
                    readOnly: true,
                    additionalViewData: null);
                result.AppendHtml(templateBuilder.Build());
            }

            return result;
        }
        finally
        {
            htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
        }
    }

    public static IHtmlContent DecimalTemplate(IHtmlHelper htmlHelper)
    {
        if (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == htmlHelper.ViewData.Model)
        {
            htmlHelper.ViewData.TemplateInfo.FormattedModelValue =
                string.Format(CultureInfo.CurrentCulture, "{0:0.00}", htmlHelper.ViewData.Model);
        }

        return StringTemplate(htmlHelper);
    }

    public static IHtmlContent EmailAddressTemplate(IHtmlHelper htmlHelper)
    {
        var uriString = "mailto:" + ((htmlHelper.ViewData.Model == null) ?
            string.Empty :
            htmlHelper.ViewData.Model.ToString());
        var linkedText = (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == null) ?
            string.Empty :
            htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString();

        return HyperlinkTemplate(uriString, linkedText);
    }

    public static IHtmlContent HiddenInputTemplate(IHtmlHelper htmlHelper)
    {
        if (htmlHelper.ViewData.ModelMetadata.HideSurroundingHtml)
        {
            return HtmlString.Empty;
        }

        return StringTemplate(htmlHelper);
    }

    public static IHtmlContent HtmlTemplate(IHtmlHelper htmlHelper)
    {
        return new HtmlString(htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString());
    }

    public static IHtmlContent ObjectTemplate(IHtmlHelper htmlHelper)
    {
        var viewData = htmlHelper.ViewData;
        var templateInfo = viewData.TemplateInfo;
        var modelExplorer = viewData.ModelExplorer;

        if (modelExplorer.Model == null)
        {
            return new HtmlString(modelExplorer.Metadata.NullDisplayText);
        }

        if (templateInfo.TemplateDepth > 1)
        {
            var text = modelExplorer.GetSimpleDisplayText();
            if (modelExplorer.Metadata.HtmlEncode)
            {
                text = htmlHelper.Encode(text);
            }

            return new HtmlString(text);
        }

        var serviceProvider = htmlHelper.ViewContext.HttpContext.RequestServices;
        var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();
        var viewBufferScope = serviceProvider.GetRequiredService<IViewBufferScope>();

        var content = new HtmlContentBuilder(modelExplorer.Metadata.Properties.Count);
        foreach (var propertyExplorer in modelExplorer.PropertiesInternal)
        {
            var propertyMetadata = propertyExplorer.Metadata;
            if (!ShouldShow(propertyExplorer, templateInfo))
            {
                continue;
            }

            var templateBuilder = new TemplateBuilder(
                viewEngine,
                viewBufferScope,
                htmlHelper.ViewContext,
                htmlHelper.ViewData,
                propertyExplorer,
                htmlFieldName: propertyMetadata.PropertyName,
                templateName: null,
                readOnly: true,
                additionalViewData: null);

            var templateBuilderResult = templateBuilder.Build();
            if (!propertyMetadata.HideSurroundingHtml)
            {
                var label = propertyMetadata.GetDisplayName();
                if (!string.IsNullOrEmpty(label))
                {
                    var labelTag = new TagBuilder("div");
                    labelTag.InnerHtml.SetContent(label);
                    labelTag.AddCssClass("display-label");
                    content.AppendLine(labelTag);
                }

                var valueDivTag = new TagBuilder("div");
                valueDivTag.AddCssClass("display-field");
                valueDivTag.InnerHtml.SetHtmlContent(templateBuilderResult);
                content.AppendLine(valueDivTag);
            }
            else
            {
                content.AppendHtml(templateBuilderResult);
            }
        }

        return content;
    }

    private static bool ShouldShow(ModelExplorer modelExplorer, TemplateInfo templateInfo)
    {
        return
            modelExplorer.Metadata.ShowForDisplay &&
            !modelExplorer.Metadata.IsComplexType &&
            !templateInfo.Visited(modelExplorer);
    }

    public static IHtmlContent StringTemplate(IHtmlHelper htmlHelper)
    {
        var value = htmlHelper.ViewData.TemplateInfo.FormattedModelValue;
        if (value == null)
        {
            return HtmlString.Empty;
        }

        return new StringHtmlContent(value.ToString());
    }

    public static IHtmlContent UrlTemplate(IHtmlHelper htmlHelper)
    {
        var uriString = (htmlHelper.ViewData.Model == null) ? string.Empty : htmlHelper.ViewData.Model.ToString();
        var linkedText = (htmlHelper.ViewData.TemplateInfo.FormattedModelValue == null) ?
            string.Empty :
            htmlHelper.ViewData.TemplateInfo.FormattedModelValue.ToString();

        return HyperlinkTemplate(uriString, linkedText);
    }

    // Neither uriString nor linkedText need be encoded prior to calling this method.
    private static IHtmlContent HyperlinkTemplate(string uriString, string linkedText)
    {
        var hyperlinkTag = new TagBuilder("a");
        hyperlinkTag.MergeAttribute("href", uriString);
        hyperlinkTag.InnerHtml.SetContent(linkedText);
        return hyperlinkTag;
    }
}
