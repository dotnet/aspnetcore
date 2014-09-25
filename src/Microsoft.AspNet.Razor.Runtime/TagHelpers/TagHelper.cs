// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to filter matching HTML elements.
    /// </summary>
    public abstract class TagHelper : ITagHelper
    {
        /// <summary>
        /// Synchronously executes the <see cref="TagHelper"/> with the given <paramref name="context"/> and
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        public virtual void Process(TagHelperContext context, TagHelperOutput output)
        {
        }

        /// <summary>
        /// Asynchronously executes the <see cref="TagHelper"/> with the given <paramref name="context"/> and
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        /// <returns>A <see cref="Task"/> that on completion updates the <paramref name="output"/>.</returns>
        /// <remarks>By default this calls into <see cref="Process"/>.</remarks>.
        #pragma warning disable 1998
        public virtual async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Process(context, output);
        }
        #pragma warning restore 1998
    }
}