// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A <see cref="TagHelper"/> used for emitting deferred component output.
    /// </summary>
    [HtmlTargetElement(TagHelperName, TagStructure = TagStructure.WithoutEndTag)]
    public class ComponentOutputTagHelper : TagHelper
    {
        private const string TagHelperName = "component-output";

        /// <summary>
        /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the name used to identify the component whose content to emit.
        /// </summary>
        [HtmlAttributeName(PrerenderingHelpers.PrerenderedNameName)]
        public string Name { get; set; }

        /// <inheritdoc/>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            var prerenderCache = PrerenderingHelpers.GetOrCreatePrerenderCache(ViewContext);

            if (!prerenderCache.TryGetValue(Name, out var content))
            {
                throw new InvalidOperationException($"No component has an output name matching '{Name}'.");
            }

            output.TagName = null;
            output.Content.SetHtmlContent(content);
        }
    }
}
