// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// <see cref="TextWriter"/> implementation which writes to a <see cref="TagHelperContent"/> instance.
    /// </summary>
    public class TagHelperContentWrapperTextWriter : TextWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagHelperContentWrapperTextWriter"/> class.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> in which output is written.</param>
        public TagHelperContentWrapperTextWriter([NotNull] Encoding encoding)
            : this(encoding, new DefaultTagHelperContent())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagHelperContentWrapperTextWriter"/> class.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> in which output is written.</param>
        /// <param name="content">The <see cref="TagHelperContent"/> to write to.</param>
        public TagHelperContentWrapperTextWriter([NotNull] Encoding encoding, [NotNull] TagHelperContent content)
        {
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
            Content.Append(value);
        }

        /// <inheritdoc />
        public override void Write(char value)
        {
            Content.Append(value.ToString());
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Content.ToString();
        }
    }
}