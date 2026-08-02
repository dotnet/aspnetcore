// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;span&gt; elements with an <c>asp-validation-for</c>
/// attribute.
/// </summary>
[HtmlTargetElement("span", Attributes = ValidationForAttributeName)]
public class ValidationMessageTagHelper : TagHelper
{
    private const string DataValidationForAttributeName = "data-valmsg-for";
    private const string ValidationForAttributeName = "asp-validation-for";

    /// <summary>
    /// Creates a new <see cref="ValidationMessageTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public ValidationMessageTagHelper(IHtmlGenerator generator)
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
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="ValidationMessageTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// Gets an expression to be evaluated against the current model.
    /// </summary>
    [HtmlAttributeName(ValidationForAttributeName)]
    public ModelExpression For { get; set; }

    /// <inheritdoc />
    /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (For != null)
        {
            // Ensure Generator does not throw due to empty "fullName" if user provided data-valmsg-for attribute.
            // Assume data-valmsg-for value is non-empty if attribute is present at all. Should align with name of
            // another tag helper e.g. an <input/> and those tag helpers bind Name.
            IDictionary<string, object> htmlAttributes = null;
            if (string.IsNullOrEmpty(For.Name) &&
                string.IsNullOrEmpty(ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix) &&
                output.Attributes.ContainsName(DataValidationForAttributeName))
            {
                htmlAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { DataValidationForAttributeName, "-non-empty-value-" },
                    };
            }

            string message = null;
            if (!output.IsContentModified)
            {
                var tagHelperContent = await output.GetChildContentAsync();

                // We check for whitespace to detect scenarios such as:
                // <span validation-for="Name">
                // </span>
                if (!tagHelperContent.IsEmptyOrWhiteSpace)
                {
                    message = tagHelperContent.GetContent();
                }
            }
            var tagBuilder = Generator.GenerateValidationMessage(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                message: message,
                tag: null,
                htmlAttributes: htmlAttributes);

            if (tagBuilder != null)
            {
                output.MergeAttributes(tagBuilder);

                // Do not update the content if another tag helper targeting this element has already done so.
                if (!output.IsContentModified && tagBuilder.HasInnerHtml)
                {
                    output.Content.SetHtmlContent(tagBuilder.InnerHtml);
                }
            }
        }
    }
}
