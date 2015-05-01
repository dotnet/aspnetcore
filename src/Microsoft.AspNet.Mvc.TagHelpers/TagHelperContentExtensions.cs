// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

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
        /// <param name="encoder">The <see cref="IHtmlEncoder"/> to use when encoding <paramref name="value"/>.</param>
        /// <param name="encoding">The character encoding in which the <paramref name="value"/> is written.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <returns><paramref name="content"/> after the write operation has completed.</returns>
        /// <remarks>
        /// <paramref name="value"/>s of type <see cref="Rendering.HtmlString"/> are written without encoding and
        /// <see cref="HelperResult.WriteTo"/> is invoked for <see cref="HelperResult"/> types. For all other types,
        /// the encoded result of <see cref="object.ToString"/> is written to the <paramref name="content"/>.
        /// </remarks>
        public static TagHelperContent Append(
            [NotNull] this TagHelperContent content,
            [NotNull] IHtmlEncoder encoder,
            [NotNull] Encoding encoding,
            object value)
        {
            using (var writer = new TagHelperContentWrapperTextWriter(encoding, content))
            {
                RazorPage.WriteTo(writer, encoder, value, escapeQuotes: true);
            }

            return content;
        }
    }
}