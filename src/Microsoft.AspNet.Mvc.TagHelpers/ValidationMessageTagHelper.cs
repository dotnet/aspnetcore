// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;span&gt; elements with <c>validation-for</c> attributes.
    /// </summary>
    [TagName("span")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class ValidationMessageTagHelper : TagHelper
    {
        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        /// <summary>
        /// Name to be validated on the current model.
        /// </summary>
        [HtmlAttributeName("validation-for")]
        public ModelExpression For { get; set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For != null)
            {
                var tagBuilder = Generator.GenerateValidationMessage(ViewContext,
                                                                     For.Name,
                                                                     message: null,
                                                                     tag: null,
                                                                     htmlAttributes: null);

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);

                    // We check for whitespace to detect scenarios such as:
                    // <span validation-for="Name">
                    // </span>
                    if (string.IsNullOrWhiteSpace(output.Content))
                    {
                        output.Content = tagBuilder.InnerHtml;
                    }
                }
            }
        }
    }
}