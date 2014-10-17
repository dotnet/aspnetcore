// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;select&gt; elements.
    /// </summary>
    [ContentBehavior(ContentBehavior.Append)]
    public class SelectTagHelper : TagHelper
    {
        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        public ModelExpression For { get; set; }

        /// <summary>
        /// Specifies that multiple options can be selected at once.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML if value is <c>multiple</c>. Converted to <c>multiple</c> or absent if
        /// value is <c>true</c> or <c>false</c>. Other values are not acceptable. Also used to determine the correct
        /// "selected" attributes for generated &lt;option&gt; elements.
        /// </remarks>
        public string Multiple { get; set; }

        /// <summary>
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </summary>
        public IEnumerable<SelectListItem> Items { get; set; }

        /// <inheritdoc />
        /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For == null)
            {
                // Regular HTML <select/> element. Just make sure Items wasn't specified.
                if (Items != null)
                {
                    var message = Resources.FormatSelectTagHelper_CannotDetermineContentWhenOnlyItemsSpecified(
                        "<select>",
                        nameof(For).ToLowerInvariant(),
                        nameof(Items).ToLowerInvariant());
                    throw new InvalidOperationException(message);
                }

                // Pass through attribute that is also a well-known HTML attribute.
                if (!string.IsNullOrEmpty(Multiple))
                {
                    output.CopyHtmlAttribute(nameof(Multiple), context);
                }
            }
            else
            {
                // Note null or empty For.Name is allowed because TemplateInfo.HtmlFieldPrefix may be sufficient.
                // IHtmlGenerator will enforce name requirements.
                var metadata = For.Metadata;
                if (metadata == null)
                {
                    throw new InvalidOperationException(Resources.FormatTagHelpers_NoProvidedMetadata(
                        "<select>",
                        nameof(For).ToLowerInvariant(),
                        nameof(IModelMetadataProvider),
                        For.Name));
                }

                bool allowMultiple;
                if (string.IsNullOrEmpty(Multiple))
                {
                    // Base allowMultiple on the instance or declared type of the expression.
                    var realModelType = For.Metadata.RealModelType;
                    allowMultiple =
                        typeof(string) != realModelType && typeof(IEnumerable).IsAssignableFrom(realModelType);
                }
                else if (string.Equals(Multiple, "multiple", StringComparison.OrdinalIgnoreCase))
                {
                    allowMultiple = true;

                    // Copy exact attribute name and value the user entered. Must be done prior to any copying from a
                    // TagBuilder. Not done in next case because "true" and "false" aren't valid for the HTML 5
                    // attribute.
                    output.CopyHtmlAttribute(nameof(Multiple), context);
                }
                else if (!bool.TryParse(Multiple.ToLowerInvariant(), out allowMultiple))
                {
                    throw new InvalidOperationException(Resources.FormatTagHelpers_InvalidValue_ThreeAcceptableValues(
                        "<select>",
                        nameof(Multiple).ToLowerInvariant(),
                        Multiple,
                        bool.FalseString.ToLowerInvariant(),
                        bool.TrueString.ToLowerInvariant(),
                        nameof(Multiple).ToLowerInvariant())); // acceptable value (as well as attribute name)
                }

                // Ensure GenerateSelect() _never_ looks anything up in ViewData.
                var items = Items ?? Enumerable.Empty<SelectListItem>();

                var tagBuilder = Generator.GenerateSelect(
                    ViewContext,
                    For.Metadata,
                    optionLabel: null,
                    name: For.Name,
                    selectList: items,
                    allowMultiple: allowMultiple,
                    htmlAttributes: null);

                if (tagBuilder != null)
                {
                    output.SelfClosing = false;
                    output.Merge(tagBuilder);
                }
            }
        }
    }
}