// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// <see cref="HtmlTextWriter"/> implementation which writes to a <see cref="TagHelperContent"/> instance.
    /// </summary>
    public class TagHelperContentWrapperTextWriter : HtmlTextWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagHelperContentWrapperTextWriter"/> class.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> in which output is written.</param>
        public TagHelperContentWrapperTextWriter(Encoding encoding)
            : this(encoding, new DefaultTagHelperContent())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagHelperContentWrapperTextWriter"/> class.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> in which output is written.</param>
        /// <param name="content">The <see cref="TagHelperContent"/> to write to.</param>
        public TagHelperContentWrapperTextWriter(Encoding encoding, TagHelperContent content)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
            Encoding = encoding;
        }

        /// <summary>
        /// The <see cref="TagHelperContent"/> this <see cref="TagHelperContentWrapperTextWriter"/> writes to.
        /// </summary>
        public TagHelperContent Content { get; }

        /// <inheritdoc />
        public override Encoding Encoding { get; }

        /// <inheritdoc />
        public override void Write(string value)
        {
            Content.AppendHtml(value);
        }

        /// <inheritdoc />
        public override void Write(char value)
        {
            Content.AppendHtml(value.ToString());
        }

        /// <inheritdoc />
        public override void Write(IHtmlContent value)
        {
            Content.Append(value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Content.ToString();
        }
    }
}