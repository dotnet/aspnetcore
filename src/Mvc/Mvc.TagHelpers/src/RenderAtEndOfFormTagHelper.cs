// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting all form elements
    /// to generate content before the form end tag.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [HtmlTargetElement("form")]
    public class RenderAtEndOfFormTagHelper : TagHelper
    {
        // This TagHelper's order must be greater than the FormTagHelper's. I.e it must be executed after
        // FormTagHelper does.
        /// <inheritdoc />
        public override int Order => -900;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
        public override void Init(TagHelperContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Push the new FormContext.
            ViewContext.FormContext = new FormContext
            {
                CanRenderAtEndOfForm = true
            };
        }

        /// <inheritdoc />
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

            await output.GetChildContentAsync();

            var formContext = ViewContext.FormContext;
            if (formContext.HasEndOfFormContent)
            {
                // Perf: Avoid allocating enumerator
                for (var i = 0; i < formContext.EndOfFormContent.Count; i++)
                {
                    output.PostContent.AppendHtml(formContext.EndOfFormContent[i]);
                }
            }

            // Reset the FormContext
            ViewContext.FormContext = new FormContext();
        }
    }
}
