// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// Extension methods for <see cref="TagHelperContent"/>.
    /// </summary>
    public static class TagHelperContentExtensions
    {
        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to given <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The <see cref="TagHelperContent"/> to write to.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> to use when encoding <paramref name="value"/>.</param>
        /// <param name="encoding">The character encoding in which the <paramref name="value"/> is written.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <returns><paramref name="content"/> after the write operation has completed.</returns>
        /// <remarks>
        /// <paramref name="value"/>s of type <see cref="Html.Abstractions.IHtmlContent"/> are written using
        /// <see cref="Html.Abstractions.IHtmlContent.WriteTo(System.IO.TextWriter, HtmlEncoder)"/>.
        /// For all other types, the encoded result of <see cref="object.ToString"/>
        /// is written to the <paramref name="content"/>.
        /// </remarks>
        public static TagHelperContent Append(
            this TagHelperContent content,
            HtmlEncoder encoder,
            Encoding encoding,
            object value)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (var writer = new TagHelperContentWrapperTextWriter(encoding, content))
            {
                using (var stringWriter = new StringWriter())
                {
                    RazorPage.WriteTo(stringWriter, encoder, value);

                    // In this case the text likely came directly from the Razor source. Since the original string is
                    // an attribute value that may have been quoted with single quotes, must handle any double quotes
                    // in the value. Writing the value out surrounded by double quotes.
                    var stringValue = stringWriter.ToString().Replace("\"", "&quot;");
                    writer.Write(stringValue);
                }
            }

            return content;
        }
    }
}