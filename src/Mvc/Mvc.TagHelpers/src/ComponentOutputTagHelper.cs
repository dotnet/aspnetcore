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
        private const string NameName = "name";

        /// <summary>
        /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the name used to identify the component whose content to emit.
        /// </summary>
        [HtmlAttributeName(NameName)]
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

            var contentStore = ComponentDeferredContentStore.GetOrCreateContentStore(ViewContext);

            if (!contentStore.TryGetValue(Name, out var content))
            {
                throw new InvalidOperationException(Resources.FormatComponentOutputTagHelper_NoMatchingName(Name));
            }

            output.TagName = null;
            output.Content.SetHtmlContent(content);
        }
    }
}
