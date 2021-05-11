// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html
{
    /// <summary>
    /// HTML content which can be written to a TextWriter.
    /// </summary>
    public interface IHtmlContent
    {
        /// <summary>
        /// Writes the content by encoding it with the specified <paramref name="encoder"/>
        /// to the specified <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to which the content is written.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> which encodes the content to be written.</param>
        void WriteTo(TextWriter writer, HtmlEncoder encoder);
    }
}