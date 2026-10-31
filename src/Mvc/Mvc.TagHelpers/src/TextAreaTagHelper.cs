// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;textarea&gt; elements with an <c>asp-for</c> attribute.
/// </summary>
[HtmlTargetElement("textarea", Attributes = ForAttributeName)]
public class TextAreaTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    /// <summary>
    /// Creates a new <see cref="TextAreaTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public TextAreaTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="TextAreaTagHelper"/>'s output.
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
    /// The name of the &lt;input&gt; element.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases. Also used to determine whether <see cref="For"/> is
    /// valid with an empty <see cref="ModelExpression.Name"/>.
    /// </remarks>
    public string Name { get; set; }

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

        var tagBuilder = Generator.GenerateTextArea(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            rows: 0,
            columns: 0,
            htmlAttributes: htmlAttributes);

        if (tagBuilder != null)
        {
            output.MergeAttributes(tagBuilder);
            if (tagBuilder.HasInnerHtml)
            {
                // Overwrite current Content to ensure expression result round-trips correctly.
                output.Content.SetHtmlContent(tagBuilder.InnerHtml);
            }
        }
    }
}
