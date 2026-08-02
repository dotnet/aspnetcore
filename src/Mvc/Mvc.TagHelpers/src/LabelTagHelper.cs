// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;label&gt; elements with an <c>asp-for</c> attribute.
/// </summary>
[HtmlTargetElement("label", Attributes = ForAttributeName)]
public class LabelTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    /// <summary>
    /// Creates a new <see cref="LabelTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public LabelTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="Rendering.ViewContext"/> of the executing view.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="LabelTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// An expression to be evaluated against the current model.
    /// </summary>
    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; }

    /// <inheritdoc />
    /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var tagBuilder = Generator.GenerateLabel(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            labelText: null,
            htmlAttributes: null);

        if (tagBuilder != null)
        {
            output.MergeAttributes(tagBuilder);

            // Do not update the content if another tag helper targeting this element has already done so.
            if (!output.IsContentModified)
            {
                // We check for whitespace to detect scenarios such as:
                // <label for="Name">
                // </label>
                var childContent = await output.GetChildContentAsync();
                if (childContent.IsEmptyOrWhiteSpace)
                {
                    // Provide default label text (if any) since there was nothing useful in the Razor source.
                    if (tagBuilder.HasInnerHtml)
                    {
                        output.Content.SetHtmlContent(tagBuilder.InnerHtml);
                    }
                }
                else
                {
                    output.Content.SetHtmlContent(childContent);
                }
            }
        }
    }
}
