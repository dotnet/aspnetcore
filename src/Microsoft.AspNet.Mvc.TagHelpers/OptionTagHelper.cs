// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
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
        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        /// <summary>
        /// Specifies that this &lt;option&gt; is pre-selected.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        public string Selected { get; set; }

        /// <summary>
        /// Specifies a value for the &lt;option&gt; element.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        public string Value { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Does nothing unless <see cref="FormContext.FormData"/> contains a
        /// <see cref="SelectTagHelper.SelectedValuesFormDataKey"/> entry and that entry is a non-empty
        /// <see cref="ICollection{string}"/> instance. Also does nothing if the associated &lt;option&gt; is already
        /// selected.
        /// </remarks>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Pass through attributes that are also well-known HTML attributes.
            if (Value != null)
            {
                output.CopyHtmlAttribute(nameof(Value), context);
            }

            if (Selected != null)
            {
                // This <option/> will always be selected.
                output.CopyHtmlAttribute(nameof(Selected), context);
            }
            else
            {
                // Is this <option/> element a child of a <select/> element the SelectTagHelper targeted?
                object formDataEntry;
                ViewContext.FormContext.FormData.TryGetValue(
                    SelectTagHelper.SelectedValuesFormDataKey,
                    out formDataEntry);

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
                    // encodes attribute values later, when GenerateStartTag() is called.
                    var text = await context.GetChildContentAsync();
                    var selected = (Value != null) ? selectedValues.Contains(Value) : encodedValues.Contains(text);
                    if (selected)
                    {
                        output.Attributes.Add("selected", "selected");
                    }
                }
            }
        }
    }
}
