// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// A <see cref="HtmlTextWriter"/> that stores individual write operations as a sequence of
    /// <see cref="string"/> and <see cref="IHtmlContent"/> instances.
    /// </summary>
    /// <remarks>
    /// This is primarily designed to avoid creating large in-memory strings.
    /// Refer to https://aspnetwebstack.codeplex.com/workitem/585 for more details.
    /// </remarks>
    public class StringCollectionTextWriter : HtmlTextWriter
    {
        private const int MaxCharToStringLength = 1024;
        private static readonly Task _completedTask = Task.FromResult(0);

        private readonly Encoding _encoding;
        private readonly StringCollectionTextWriterContent _content;

        /// <summary>
        /// Creates a new instance of <see cref="StringCollectionTextWriter"/>.
        /// </summary>
        /// <param name="encoding">The character <see cref="Encoding"/> in which the output is written.</param>
        public StringCollectionTextWriter(Encoding encoding)
        {
            _encoding = encoding;
            Entries = new List<object>();
            _content = new StringCollectionTextWriterContent(Entries);
        }

        /// <inheritdoc />
        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        /// <summary>
        /// Gets the content written to the writer as an <see cref="IHtmlContent"/>.
        /// </summary>
        public IHtmlContent Content => _content;

        // internal for testing purposes.
        internal List<object> Entries { get; }

        /// <inheritdoc />
        public override void Write(char value)
        {
            _content.Append(value.ToString());
        }

        /// <inheritdoc />
        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0 || (buffer.Length - index < count))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (count > 0)
            {
                // Split large char arrays into 1KB strings.
                var currentCount = count;
                if (MaxCharToStringLength < currentCount)
                {
                    currentCount = MaxCharToStringLength;
                }

                _content.Append(new string(buffer, index, currentCount));
                index += currentCount;
                count -= currentCount;
            }
        }

        /// <inheritdoc />
        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _content.Append(value);
        }

        /// <inheritdoc />
        public override void Write(IHtmlContent value)
        {
            _content.Append(value);
        }

        /// <inheritdoc />
        public override Task WriteAsync(char value)
        {
            Write(value);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Write(buffer, index, count);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteAsync(string value)
        {
            Write(value);
            return _completedTask;
        }

        /// <inheritdoc />
        public override void WriteLine()
        {
            _content.Append(Environment.NewLine);
        }

        /// <inheritdoc />
        public override void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char value)
        {
            WriteLine(value);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char[] value, int start, int offset)
        {
            WriteLine(value, start, offset);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(string value)
        {
            WriteLine(value);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteLineAsync()
        {
            WriteLine();
            return _completedTask;
        }

        /// <summary>
        /// If the specified <paramref name="writer"/> is a <see cref="StringCollectionTextWriter"/> the contents
        /// are copied. It is just written to the <paramref name="writer"/> otherwise.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to which the content must be copied/written.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> to encode the copied/written content.</param>
        public void CopyTo(TextWriter writer, HtmlEncoder encoder)
        {
            var targetStringCollectionWriter = writer as StringCollectionTextWriter;
            if (targetStringCollectionWriter != null)
            {
                targetStringCollectionWriter._content.Append(Content);
            }
            else
            {
                Content.WriteTo(writer, encoder);
            }
        }

        /// <summary>
        /// If the specified <paramref name="writer"/> is a <see cref="StringCollectionTextWriter"/> the contents
        /// are copied. It is just written to the <paramref name="writer"/> otherwise.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to which the content must be copied/written.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> to encode the copied/written content.</param>
        public Task CopyToAsync(TextWriter writer, HtmlEncoder encoder)
        {
            CopyTo(writer, encoder);
            return _completedTask;
        }

        [DebuggerDisplay("{DebuggerToString()}")]
        private class StringCollectionTextWriterContent : IHtmlContent
        {
            private readonly List<object> _entries;

            public StringCollectionTextWriterContent(List<object> entries)
            {
                _entries = entries;
            }

            public void Append(string value)
            {
                _entries.Add(value);
            }

            public void Append(IHtmlContent content)
            {
                _entries.Add(content);
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                foreach (var item in _entries)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var itemAsString = item as string;
                    if (itemAsString != null)
                    {
                        writer.Write(itemAsString);
                    }
                    else
                    {
                        ((IHtmlContent)item).WriteTo(writer, encoder);
                    }
                }
            }

            private string DebuggerToString()
            {
                using (var writer = new StringWriter())
                {
                    WriteTo(writer, HtmlEncoder.Default);
                    return writer.ToString();
                }
            }
        }
    }
}