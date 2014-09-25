// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contract used to filter matching HTML elements.
    /// </summary>
    public interface ITagHelper
    {
        /// <summary>
        /// Asynchronously executes the <see cref="ITagHelper"/> with the given <paramref name="context"/> and
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        /// <returns>A <see cref="Task"/> that on completion updates the <paramref name="output"/>.</returns>
        Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
    }
}