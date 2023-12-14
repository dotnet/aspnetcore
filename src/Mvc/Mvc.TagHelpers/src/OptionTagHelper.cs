// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;option&gt; elements.
/// </summary>
/// <remarks>
/// This <see cref="ITagHelper"/> works in conjunction with <see cref="SelectTagHelper"/>. It reads elements
/// content but does not modify that content. The only modification it makes is to add a <c>selected</c> attribute
/// in some cases.
/// </remarks>
public class OptionTagHelper : TagHelper
{
    /// <summary>
    /// Creates a new <see cref="OptionTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public OptionTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="OptionTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// Gets the <see cref="Rendering.ViewContext"/> of the executing view.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Specifies a value for the &lt;option&gt; element.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases.
    /// </remarks>
    public string Value { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// Does nothing unless <see cref="TagHelperContext.Items"/> contains a
    /// <see cref="SelectTagHelper"/> <see cref="Type"/> entry and that entry is a non-<c>null</c>
    /// <see cref="CurrentValues"/> instance. Also does nothing if the associated &lt;option&gt; is already
    /// selected.
    /// </remarks>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // Pass through attributes that are also well-known HTML attributes.
        if (Value != null)
        {
            output.CopyHtmlAttribute(nameof(Value), context);
        }

        // Nothing to do if this <option/> is already selected.
        if (!output.Attributes.ContainsName("selected"))
        {
            // Is this <option/> element a child of a <select/> element the SelectTagHelper targeted?
            context.Items.TryGetValue(typeof(SelectTagHelper), out var formDataEntry);

            // ... And did the SelectTagHelper determine any selected values?
            var currentValues = formDataEntry as CurrentValues;
            if (currentValues?.Values != null && currentValues.Values.Count != 0)
            {
                // Select this <option/> element if value attribute or (if no value attribute) body matches a
                // selected value. Body is encoded as-needed while executing child content. But TagHelperOutput
                // itself encodes attribute values later, when start tag is generated.
                bool selected;
                if (Value != null)
                {
                    selected = currentValues.Values.Contains(Value);
                }
                else
                {
                    if (currentValues.ValuesAndEncodedValues == null)
                    {
                        // Include encoded versions of all selected values when comparing with body.
                        var allValues = new HashSet<string>(currentValues.Values, StringComparer.OrdinalIgnoreCase);
                        foreach (var selectedValue in currentValues.Values)
                        {
                            allValues.Add(Generator.Encode(selectedValue));
                        }

                        currentValues.ValuesAndEncodedValues = allValues;
                    }

                    TagHelperContent childContent;
                    if (output.IsContentModified)
                    {
                        // Another tag helper has modified the body. Use what they wrote.
                        childContent = output.Content;
                    }
                    else
                    {
                        childContent = await output.GetChildContentAsync();
                    }

                    selected = currentValues.ValuesAndEncodedValues.Contains(childContent.GetContent());
                }

                if (selected)
                {
                    output.Attributes.Add("selected", "selected");
                }
            }
        }
    }
}
