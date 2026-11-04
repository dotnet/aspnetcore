// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;div&gt; elements with an <c>asp-validation-summary</c>
/// attribute.
/// </summary>
[HtmlTargetElement("div", Attributes = ValidationSummaryAttributeName)]
public class ValidationSummaryTagHelper : TagHelper
{
    private const string ValidationSummaryAttributeName = "asp-validation-summary";
    private ValidationSummary _validationSummary;

    /// <summary>
    /// Creates a new <see cref="ValidationSummaryTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public ValidationSummaryTagHelper(IHtmlGenerator generator)
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
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="ValidationSummaryTagHelper"/>'s output.
    /// </summary>
    [HtmlAttributeNotBound]
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// If <see cref="ValidationSummary.All"/> or <see cref="ValidationSummary.ModelOnly"/>, appends a validation
    /// summary. Otherwise (<see cref="ValidationSummary.None"/>, the default), this tag helper does nothing.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if setter is called with an undefined <see cref="ValidationSummary"/> value e.g.
    /// <c>(ValidationSummary)23</c>.
    /// </exception>
    [HtmlAttributeName(ValidationSummaryAttributeName)]
    public ValidationSummary ValidationSummary
    {
        get => _validationSummary;
        set
        {
            switch (value)
            {
                case ValidationSummary.All:
                case ValidationSummary.ModelOnly:
                case ValidationSummary.None:
                    _validationSummary = value;
                    break;

                default:
                    throw new ArgumentException(
                        message: Resources.FormatInvalidEnumArgument(
                            nameof(value),
                            value,
                            typeof(ValidationSummary).FullName),
                        paramName: nameof(value));
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>Does nothing if <see cref="ValidationSummary"/> is <see cref="ValidationSummary.None"/>.</remarks>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (ValidationSummary == ValidationSummary.None)
        {
            return;
        }

        var tagBuilder = Generator.GenerateValidationSummary(
            ViewContext,
            excludePropertyErrors: ValidationSummary == ValidationSummary.ModelOnly,
            message: null,
            headerTag: null,
            htmlAttributes: null);
        if (tagBuilder == null)
        {
            // The generator determined no element was necessary.
            output.SuppressOutput();
            return;
        }

        output.MergeAttributes(tagBuilder);
        if (tagBuilder.HasInnerHtml)
        {
            output.PostContent.AppendHtml(tagBuilder.InnerHtml);
        }
    }
}
