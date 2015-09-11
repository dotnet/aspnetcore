// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Html.Abstractions
{
    /// <summary>
    /// A builder for HTML content.
    /// </summary>
    public interface IHtmlContentBuilder : IHtmlContent
    {
        /// <summary>
        /// Appends an <see cref="IHtmlContent"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="IHtmlContent"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder Append(IHtmlContent content);

        /// <summary>
        /// Appends a <see cref="string"/> value. The value is treated as unencoded as-provided, and will be HTML
        /// encoded before writing to output.
        /// </summary>
        /// <param name="content">The <see cref="string"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder Append(string unencoded);

        /// <summary>
        /// Clears the content.
        /// </summary>
        void Clear();
    }
}
