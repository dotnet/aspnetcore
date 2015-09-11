// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Default concrete <see cref="TagHelperContent"/>.
    /// </summary>
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
        public override TagHelperContent Append(string value)
        {
            Buffer.Append(value);
            return this;
        }

        public override TagHelperContent AppendEncoded(string value)
        {
            Buffer.AppendEncoded(value);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0)
        {
            Buffer.Append(string.Format(format, arg0));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0, object arg1)
        {
            Buffer.Append(string.Format(format, arg0, arg1));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0, object arg1, object arg2)
        {
            Buffer.Append(string.Format(format, arg0, arg1, arg2));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, params object[] args)
        {
            Buffer.Append(string.Format(format, args));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            object arg0)
        {
            Buffer.Append(string.Format(provider, format, arg0));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            object arg0,
            object arg1)
        {
            Buffer.Append(string.Format(provider, format, arg0, arg1));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            object arg0,
            object arg1,
            object arg2)
        {
            Buffer.Append(string.Format(provider, format, arg0, arg1, arg2));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            params object[] args)
        {
            Buffer.Append(string.Format(provider, format, args));
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
            if (_buffer == null)
            {
                return string.Empty;
            }

            using (var writer = new StringWriter())
            {
                WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        /// <inheritdoc />
        public override void WriteTo([NotNull] TextWriter writer, [NotNull] IHtmlEncoder encoder)
        {
            Buffer.WriteTo(writer, encoder);
        }

        /// <inheritdoc />
        public override string ToString()
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

#if DNXCORE50
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