// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
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
        public override int Order
        {
            get
            {
                return -1000;
            }
        }

        protected IHtmlGenerator Generator { get; }

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
        /// <see cref="SelectTagHelper"/> <see cref="Type"/> entry and that entry is a non-empty
        /// <see cref="ICollection{string}"/> instance. Also does nothing if the associated &lt;option&gt; is already
        /// selected.
        /// </remarks>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            // Pass through attributes that are also well-known HTML attributes.
            if (Value != null)
            {
                output.CopyHtmlAttribute(nameof(Value), context);
            }

            // Nothing to do if this <option/> is already selected.
            if (!output.Attributes.ContainsName("selected"))
            {
                // Is this <option/> element a child of a <select/> element the SelectTagHelper targeted?
                object formDataEntry;
                context.Items.TryGetValue(typeof(SelectTagHelper), out formDataEntry);

                // ... And did the SelectTagHelper determine any selected values?
                var selectedValues = formDataEntry as ICollection<string>;
                if (selectedValues != null && selectedValues.Count != 0)
                {
                    // Encode all selected values for comparison with element content.
                    var encodedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var selectedValue in selectedValues)
                    {
                        encodedValues.Add(Generator.Encode(selectedValue));
                    }

                    // Select this <option/> element if value attribute or content matches a selected value. Callers
                    // encode values as-needed while executing child content. But TagHelperOutput itself
                    // encodes attribute values later, when start tag is generated.
                    bool selected;
                    if (Value != null)
                    {
                        selected = selectedValues.Contains(Value);
                    }
                    else if (output.IsContentModified)
                    {
                        selected = encodedValues.Contains(output.Content.GetContent());
                    }
                    else
                    {
                        var childContent = await output.GetChildContentAsync();
                        selected = encodedValues.Contains(childContent.GetContent());
                    }

                    if (selected)
                    {
                        output.Attributes.Add("selected", "selected");
                    }
                }
            }
        }
    }
}
