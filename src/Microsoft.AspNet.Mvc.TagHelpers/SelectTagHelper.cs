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
    /// <see cref="ITagHelper"/> implementation targeting &lt;select&gt; elements with an <c>asp-for</c> attribute.
    /// </summary>
    public class SelectTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string ItemsAttributeName = "asp-items";

        /// <summary>
        /// Key used for selected values in <see cref="FormContext.FormData"/>.
        /// </summary>
        /// <remarks>
        /// Value for this dictionary entry will either be <c>null</c> (indicating no <see cref="SelectTagHelper"/> has
        /// executed within this &lt;form/&gt;) or an <see cref="ICollection{string}"/> instance. Elements of the
        /// collection are based on current <see cref="ViewDataDictionary.Model"/>.
        /// </remarks>
        public static readonly string SelectedValuesFormDataKey = nameof(SelectTagHelper) + "-SelectedValues";

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
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
        [HtmlAttributeName(ItemsAttributeName)]
        public IEnumerable<SelectListItem> Items { get; set; }

        /// <inheritdoc />
        /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Items"/> is non-<c>null</c> but <see cref="For"/> is <c>null</c>.
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For == null)
            {
                // Regular HTML <select/> element. Just make sure Items wasn't specified.
                if (Items != null)
                {
                    var message = Resources.FormatSelectTagHelper_CannotDetermineContentWhenOnlyItemsSpecified(
                        "<select>",
                        ForAttributeName,
                        ItemsAttributeName);
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
                        ForAttributeName,
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

                ICollection<string> selectedValues;
                var tagBuilder = Generator.GenerateSelect(
                    ViewContext,
                    For.Metadata,
                    optionLabel: null,
                    name: For.Name,
                    selectList: items,
                    allowMultiple: allowMultiple,
                    htmlAttributes: null,
                    selectedValues: out selectedValues);

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);
                    output.PostContent += tagBuilder.InnerHtml;
                    output.SelfClosing = false;
                }

                // Whether or not (not being highly unlikely) we generate anything, could update contained <option/>
                // elements. Provide selected values for <option/> tag helpers. They'll run next.
                ViewContext.FormContext.FormData[SelectedValuesFormDataKey] = selectedValues;
            }
        }
    }
}