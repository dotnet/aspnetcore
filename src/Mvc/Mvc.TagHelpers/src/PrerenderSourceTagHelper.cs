// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A <see cref="TagHelper"/> used for rendering components and deferring their output.
    /// </summary>
    [HtmlTargetElement(TagHelperName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class PrerenderSourceTagHelper : TagHelper
    {
        internal const string TagHelperName = "prerender-source";

        /// <inheritdoc/>
        public override void Init(TagHelperContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Items[typeof(PrerenderSourceTagHelper)] = this;
        }

        /// <inheritdoc/>
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

            // We get the child content here to process any child root components, whose
            // content is cached in the ViewContext. As such, we don't need to emit any content.
            await output.GetChildContentAsync();

            output.TagName = null;
            output.Content.SetHtmlContent(string.Empty);
        }
    }
}
