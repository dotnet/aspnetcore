// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Html
{
    /// <summary>
    /// A builder for HTML content.
    /// </summary>
    public interface IHtmlContentBuilder : IHtmlContentContainer
    {
        /// <summary>
        /// Appends an <see cref="IHtmlContent"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="IHtmlContent"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder AppendHtml(IHtmlContent content);

        /// <summary>
        /// Appends a <see cref="string"/> value. The value is treated as unencoded as-provided, and will be HTML
        /// encoded before writing to output.
        /// </summary>
        /// <param name="unencoded">The <see cref="string"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder Append(string unencoded);

        /// <summary>
        /// Appends an HTML encoded <see cref="string"/> value. The value is treated as HTML encoded as-provided, and
        /// no further encoding will be performed.
        /// </summary>
        /// <param name="encoded">The HTML encoded <see cref="string"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder AppendHtml(string encoded);

        /// <summary>
        /// Clears the content.
        /// </summary>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        IHtmlContentBuilder Clear();
    }
}
