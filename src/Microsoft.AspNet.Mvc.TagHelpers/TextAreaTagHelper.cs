// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;textarea&gt; elements.
    /// </summary>
    [ContentBehavior(ContentBehavior.Replace)]
    public class TextAreaTagHelper : TagHelper
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

        /// <inheritdoc />
        /// <remarks>Does nothing unless user binds "for" attribute in Razor source.</remarks>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For != null)
            {
                var tagBuilder = Generator.GenerateTextArea(
                    ViewContext,
                    For.Metadata,
                    For.Name,
                    rows: 0,
                    columns: 0,
                    htmlAttributes: null);

                if (tagBuilder != null)
                {
                    // Overwrite current Content to ensure expression result round-trips correctly.
                    output.Content = tagBuilder.InnerHtml;

                    output.MergeAttributes(tagBuilder);
                    output.SelfClosing = false;
                    output.TagName = tagBuilder.TagName;
                }
            }
        }
    }
}