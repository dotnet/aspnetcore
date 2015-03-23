// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;select&gt; elements with an <c>asp-for</c> attribute.
    /// </summary>
    [TargetElement("select", Attributes = ForAttributeName)]
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

            // Base allowMultiple on the instance or declared type of the expression to avoid a
            // "SelectExpressionNotEnumerable" InvalidOperationException during generation.
            // Metadata.IsCollectionType() is similar but does not take runtime type into account.
            var realModelType = For.ModelExplorer.ModelType;
            var allowMultiple = typeof(string) != realModelType && typeof(IEnumerable).IsAssignableFrom(realModelType);

            // Ensure GenerateSelect() _never_ looks anything up in ViewData.
            var items = Items ?? Enumerable.Empty<SelectListItem>();

            var currentValues = Generator.GetCurrentValues(
                ViewContext,
                For.ModelExplorer,
                expression: For.Name,
                allowMultiple: allowMultiple);
            var tagBuilder = Generator.GenerateSelect(
                ViewContext,
                For.ModelExplorer,
                optionLabel: null,
                expression: For.Name,
                selectList: items,
                currentValues: currentValues,
                allowMultiple: allowMultiple,
                htmlAttributes: null);

            if (tagBuilder != null)
            {
                output.MergeAttributes(tagBuilder);
                output.PostContent.Append(tagBuilder.InnerHtml);
            }

            // Whether or not (not being highly unlikely) we generate anything, could update contained <option/>
            // elements. Provide selected values for <option/> tag helpers. They'll run next.
            ViewContext.FormContext.FormData[SelectedValuesFormDataKey] = currentValues;
        }
    }
}