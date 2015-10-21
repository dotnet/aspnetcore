// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Default concrete <see cref="TagHelperContent"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class DefaultTagHelperContent : TagHelperContent
    {
        private BufferedHtmlContent _buffer;

        private BufferedHtmlContent Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = new BufferedHtmlContent();
                }

                return _buffer;
            }
        }

        /// <inheritdoc />
        public override bool IsModified => _buffer != null;

        /// <inheritdoc />
        /// <remarks>Returns <c>true</c> for a cleared <see cref="TagHelperContent"/>.</remarks>
        public override bool IsWhiteSpace
        {
            get
            {
                if (_buffer == null)
                {
                    return true;
                }

                using (var writer = new EmptyOrWhiteSpaceWriter())
                {
                    foreach (var entry in _buffer.Entries)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrWhiteSpace(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsWhiteSpace)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override bool IsEmpty
        {
            get
            {
                if (_buffer == null)
                {
                    return true;
                }

                using (var writer = new EmptyOrWhiteSpaceWriter())
                {
                    foreach (var entry in _buffer.Entries)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        var stringValue = entry as string;
                        if (stringValue != null)
                        {
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            ((IHtmlContent)entry).WriteTo(writer, HtmlEncoder.Default);
                            if (!writer.IsEmpty)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override TagHelperContent Append(string unencoded)
        {
            Buffer.Append(unencoded);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendHtml(string encoded)
        {
            Buffer.AppendHtml(encoded);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent Append(IHtmlContent htmlContent)
        {
            Buffer.Append(htmlContent);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent Clear()
        {
            Buffer.Clear();
            return this;
        }

        /// <inheritdoc />
        public override string GetContent()
        {
            return GetContent(HtmlEncoder.Default);
        }

        /// <inheritdoc />
        public override string GetContent(IHtmlEncoder encoder)
        {
            if (_buffer == null)
            {
                return string.Empty;
            }

            using (var writer = new StringWriter())
            {
                WriteTo(writer, encoder);
                return writer.ToString();
            }
        }

        /// <inheritdoc />
        public override void WriteTo(TextWriter writer, IHtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            Buffer.WriteTo(writer, encoder);
        }

        private string DebuggerToString()
        {
            return GetContent();
        }

        // Overrides Write(string) to find if the content written is empty/whitespace.
        private class EmptyOrWhiteSpaceWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsEmpty { get; private set; } = true;

            public bool IsWhiteSpace { get; private set; } = true;

#if DOTNET5_4
            // This is an abstract method in DNXCore
            public override void Write(char value)
            {
                throw new NotImplementedException();
            }
#endif

            public override void Write(string value)
            {
                if (IsEmpty && !string.IsNullOrEmpty(value))
                {
                    IsEmpty = false;
                }

                if (IsWhiteSpace && !string.IsNullOrWhiteSpace(value))
                {
                    IsWhiteSpace = false;
                }
            }
        }
    }
}