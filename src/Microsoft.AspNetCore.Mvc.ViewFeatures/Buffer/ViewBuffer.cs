// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Buffer
{
    /// <summary>
    /// An <see cref="IHtmlContentBuilder"/> that is backed by a buffer provided by <see cref="IViewBufferScope"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public class ViewBuffer : IHtmlContentBuilder
    {
        private readonly IViewBufferScope _bufferScope;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewBuffer"/>.
        /// </summary>
        /// <param name="bufferScope">The <see cref="IViewBufferScope"/>.</param>
        /// <param name="name">A name to identify this instance.</param>
        public ViewBuffer(IViewBufferScope bufferScope, string name)
        {
            if (bufferScope == null)
            {
                throw new ArgumentNullException(nameof(bufferScope));
            }

            _bufferScope = bufferScope;
            _name = name;
        }

        /// <summary>
        /// Gets the backing buffer.
        /// </summary>
        public IList<ViewBufferValue[]> BufferSegments { get; private set; }

        /// <summary>
        /// Gets the count of entries in the last element of <see cref="BufferSegments"/>.
        /// </summary>
        public int CurrentCount { get; private set; }

        /// <inheritdoc />
        public IHtmlContentBuilder Append(string unencoded)
        {
            if (unencoded == null)
            {
                return this;
            }

            AppendValue(new ViewBufferValue(unencoded));
            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(IHtmlContent content)
        {
            if (content == null)
            {
                return this;
            }

            AppendValue(new ViewBufferValue(content));
            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(string encoded)
        {
            if (encoded == null)
            {
                return this;
            }

            var value = new HtmlString(encoded);
            AppendValue(new ViewBufferValue(value));
            return this;
        }

        private void AppendValue(ViewBufferValue value)
        {
            ViewBufferValue[] segment;
            if (BufferSegments == null)
            {
                BufferSegments = new List<ViewBufferValue[]>(1);
                segment = _bufferScope.GetSegment();
                BufferSegments.Add(segment);
            }
            else
            {
                segment = BufferSegments[BufferSegments.Count - 1];
                if (CurrentCount == segment.Length)
                {
                    segment = _bufferScope.GetSegment();
                    BufferSegments.Add(segment);
                    CurrentCount = 0;
                }
            }

            segment[CurrentCount] = value;
            CurrentCount++;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Clear()
        {
            if (BufferSegments != null)
            {
                CurrentCount = 0;
                BufferSegments = null;
            }

            return this;
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (BufferSegments == null)
            {
                return;
            }

            var htmlTextWriter = writer as HtmlTextWriter;
            if (htmlTextWriter != null)
            {
                htmlTextWriter.Write(this);
                return;
            }

            for (var i = 0; i < BufferSegments.Count; i++)
            {
                var segment = BufferSegments[i];
                var count = i == BufferSegments.Count - 1 ? CurrentCount : segment.Length;

                for (var j = 0; j < count; j++)
                {
                    var value = segment[j];

                    var valueAsString = value.Value as string;
                    if (valueAsString != null)
                    {
                        writer.Write(valueAsString);
                        continue;
                    }

                    var valueAsHtmlContent = value.Value as IHtmlContent;
                    if (valueAsHtmlContent != null)
                    {
                        valueAsHtmlContent.WriteTo(writer, encoder);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the buffered content to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/>.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete once content has been written.</returns>
        public async Task WriteToAsync(TextWriter writer, HtmlEncoder encoder)
        {
            if (BufferSegments == null)
            {
                return;
            }

            var htmlTextWriter = writer as HtmlTextWriter;
            if (htmlTextWriter != null)
            {
                htmlTextWriter.Write(this);
                return;
            }

            for (var i = 0; i < BufferSegments.Count; i++)
            {
                var segment = BufferSegments[i];
                var count = i == BufferSegments.Count - 1 ? CurrentCount : segment.Length;

                for (var j = 0; j < count; j++)
                {
                    var value = segment[j];

                    var valueAsString = value.Value as string;
                    if (valueAsString != null)
                    {
                        await writer.WriteAsync(valueAsString);
                        continue;
                    }

                    var valueAsViewBuffer = value.Value as ViewBuffer;
                    if (valueAsViewBuffer != null)
                    {
                        await valueAsViewBuffer.WriteToAsync(writer, encoder);
                        continue;
                    }

                    var valueAsHtmlContent = value.Value as IHtmlContent;
                    if (valueAsHtmlContent != null)
                    {
                        valueAsHtmlContent.WriteTo(writer, encoder);
                        continue;
                    }
                }
            }
        }

        private string DebuggerToString() => _name;
    }
}
