// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;select&gt; elements with <c>asp-for</c> and/or
/// <c>asp-items</c> attribute(s).
/// </summary>
[HtmlTargetElement("select", Attributes = ForAttributeName)]
[HtmlTargetElement("select", Attributes = ItemsAttributeName)]
public class SelectTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string ItemsAttributeName = "asp-items";
    private bool _allowMultiple;
    private ICollection<string> _currentValues;

    /// <summary>
    /// Creates a new <see cref="SelectTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public SelectTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="SelectTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// Gets the <see cref="Rendering.ViewContext"/> of the executing view.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// An expression to be evaluated against the current model.
    /// </summary>
    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; }

    /// <summary>
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements.
    /// </summary>
    [HtmlAttributeName(ItemsAttributeName)]
    public IEnumerable<SelectListItem> Items { get; set; }

    /// <summary>
    /// The name of the &lt;input&gt; element.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases. Also used to determine whether <see cref="For"/> is
    /// valid with an empty <see cref="ModelExpression.Name"/>.
    /// </remarks>
    public string Name { get; set; }

    /// <inheritdoc />
    public override void Init(TagHelperContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (For == null)
        {
            // Informs contained elements that they're running within a targeted <select/> element.
            context.Items[typeof(SelectTagHelper)] = null;
            return;
        }

        // Note null or empty For.Name is allowed because TemplateInfo.HtmlFieldPrefix may be sufficient.
        // IHtmlGenerator will enforce name requirements.
        if (For.Metadata == null)
        {
            throw new InvalidOperationException(Resources.FormatTagHelpers_NoProvidedMetadata(
                "<select>",
                ForAttributeName,
                nameof(IModelMetadataProvider),
                For.Name));
        }

        // Base allowMultiple on the instance or declared type of the expression to avoid a
        // "SelectExpressionNotEnumerable" InvalidOperationException during generation.
        // Metadata.IsEnumerableType is similar but does not take runtime type into account.
        var realModelType = For.ModelExplorer.ModelType;
        _allowMultiple = typeof(string) != realModelType &&
            typeof(IEnumerable).IsAssignableFrom(realModelType);
        _currentValues = Generator.GetCurrentValues(ViewContext, For.ModelExplorer, For.Name, _allowMultiple);

        // Whether or not (not being highly unlikely) we generate anything, could update contained <option/>
        // elements. Provide selected values for <option/> tag helpers.
        var currentValues = _currentValues == null ? null : new CurrentValues(_currentValues);
        context.Items[typeof(SelectTagHelper)] = currentValues;
    }

    /// <inheritdoc />
    /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // Pass through attribute that is also a well-known HTML attribute. Must be done prior to any copying
        // from a TagBuilder.
        if (Name != null)
        {
            output.CopyHtmlAttribute(nameof(Name), context);
        }

        // Ensure GenerateSelect() _never_ looks anything up in ViewData.
        var items = Items ?? Enumerable.Empty<SelectListItem>();

        if (For == null)
        {
            var options = Generator.GenerateGroupsAndOptions(optionLabel: null, selectList: items);
            output.PostContent.AppendHtml(options);
            return;
        }

        // Ensure Generator does not throw due to empty "fullName" if user provided a name attribute.
        IDictionary<string, object> htmlAttributes = null;
        if (string.IsNullOrEmpty(For.Name) &&
            string.IsNullOrEmpty(ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix) &&
            !string.IsNullOrEmpty(Name))
        {
            htmlAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "name", Name },
                };
        }

        var tagBuilder = Generator.GenerateSelect(
            ViewContext,
            For.ModelExplorer,
            optionLabel: null,
            expression: For.Name,
            selectList: items,
            currentValues: _currentValues,
            allowMultiple: _allowMultiple,
            htmlAttributes: htmlAttributes);

        if (tagBuilder != null)
        {
            output.MergeAttributes(tagBuilder);
            if (tagBuilder.HasInnerHtml)
            {
                output.PostContent.AppendHtml(tagBuilder.InnerHtml);
            }
        }
    }
}
