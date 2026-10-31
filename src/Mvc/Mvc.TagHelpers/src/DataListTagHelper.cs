// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;datalist&gt; elements
/// with an <c>asp-items</c> attribute.
/// </summary>
[HtmlTargetElement("datalist", Attributes = ItemsAttributeName)]
public class DataListTagHelper : TagHelper
{
    private const string ItemsAttributeName = "asp-items";

    /// <summary>
    /// Creates a new <see cref="DataListTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public DataListTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the
    /// <see cref="DataListTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// A collection of <see cref="SelectListItem"/> objects used to populate
    /// the &lt;datalist&gt; element with &lt;option&gt; elements.
    /// </summary>
    [HtmlAttributeName(ItemsAttributeName)]
    public IEnumerable<SelectListItem> Items { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var items = Items ?? Enumerable.Empty<SelectListItem>();

        var options = Generator.GenerateGroupsAndOptions(
            optionLabel: null,
            selectList: items);

        output.PostContent.AppendHtml(options);
    }
}
