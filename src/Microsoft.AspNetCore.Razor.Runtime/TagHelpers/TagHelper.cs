// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Internal;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Class used to filter matching HTML elements.
    /// </summary>
    public abstract class TagHelper : ITagHelper
    {
        /// <inheritdoc />
        /// <remarks>Default order is <c>0</c>.</remarks>
        public virtual int Order { get; } = 0;

        /// <inheritdoc />
        public virtual void Init(TagHelperContext context)
        {
        }

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
        public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Process(context, output);
            return TaskCache.CompletedTask;
        }
    }
}