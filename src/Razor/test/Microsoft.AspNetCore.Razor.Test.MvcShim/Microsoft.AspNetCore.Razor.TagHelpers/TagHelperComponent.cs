// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// An abstract base class for <see cref="ITagHelperComponent"/>.
    /// </summary>
    public abstract class TagHelperComponent : ITagHelperComponent
    {
        /// <inheritdoc />
        /// <remarks>Default order is <c>0</c>.</remarks>
        public virtual int Order => 0;

        /// <inheritdoc />
        public virtual void Init(TagHelperContext context)
        {
        }

        /// <summary>
        /// Synchronously executes the <see cref="ITagHelperComponent"/> with the given <paramref name="context"/> and
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        public virtual void Process(TagHelperContext context, TagHelperOutput output)
        {
        }

        /// <inheritdoc />
        public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Process(context, output);
            return Task.CompletedTask;
        }
    }
}
