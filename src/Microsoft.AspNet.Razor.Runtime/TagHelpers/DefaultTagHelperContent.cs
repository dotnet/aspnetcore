// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Default concrete <see cref="TagHelperContent"/>.
    /// </summary>
    public class DefaultTagHelperContent : TagHelperContent
    {
        private readonly BufferEntryCollection _buffer;

        /// <summary>
        /// Instantiates a new instance of <see cref="DefaultTagHelperContent"/>.
        /// </summary>
        public DefaultTagHelperContent()
        {
            _buffer = new BufferEntryCollection();
        }

        /// <inheritdoc />
        public override bool IsModified
        {
            get
            {
                return _buffer.IsModified;
            }
        }

        /// <inheritdoc />
        public override bool IsWhiteSpace
        {
            get
            {
                foreach (var value in _buffer)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return false;
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
                foreach (var value in _buffer)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override TagHelperContent SetContent(string value)
        {
            Clear();
            Append(value);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent SetContent(TagHelperContent tagHelperContent)
        {
            Clear();
            Append(tagHelperContent);
            return this;
        }


        /// <inheritdoc />
        public override TagHelperContent Append(string value)
        {
            _buffer.Add(value);
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0)
        {
            _buffer.Add(string.Format(format, arg0));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0, object arg1)
        {
            _buffer.Add(string.Format(format, arg0, arg1));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, object arg0, object arg1, object arg2)
        {
            _buffer.Add(string.Format(format, arg0, arg1, arg2));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat([NotNull] string format, params object[] args)
        {
            _buffer.Add(string.Format(format, args));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            object arg0)
        {
            _buffer.Add(string.Format(provider, format, arg0));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            object arg0,
            object arg1)
        {
            _buffer.Add(string.Format(provider, format, arg0, arg1));
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
            _buffer.Add(string.Format(provider, format, arg0, arg1, arg2));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent AppendFormat(
            [NotNull] IFormatProvider provider,
            [NotNull] string format,
            params object[] args)
        {
            _buffer.Add(string.Format(provider, format, args));
            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent Append(TagHelperContent tagHelperContent)
        {
            if (tagHelperContent != null)
            {
                foreach (var value in tagHelperContent)
                {
                    Append(value);
                }
            }

            // If Append() was called with an empty TagHelperContent IsModified should
            // still be true. If the content was not already modified, it means it is empty.
            // So the Clear() method can be used to indirectly set the IsModified.
            if (!IsModified)
            {
                Clear();
            }

            return this;
        }

        /// <inheritdoc />
        public override TagHelperContent Clear()
        {
            _buffer.Clear();
            return this;
        }

        /// <inheritdoc />
        public override string GetContent()
        {
            return string.Join(string.Empty, _buffer);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetContent();
        }

        /// <inheritdoc />
        public override IEnumerator<string> GetEnumerator()
        {
            // The enumerator is exposed so that SetContent(TagHelperContent) and Append(TagHelperContent)
            // can use this to iterate through the values of the buffer.
            return _buffer.GetEnumerator();
        }

        /// <inheritdoc />
        public override void WriteTo(TextWriter writer, IHtmlEncoder encoder)
        {
            foreach (var entry in _buffer)
            {
                writer.Write(entry);
            }
        }
    }
}