// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    /// <summary>
    /// An <see cref="IHtmlContentBuilder"/> that is backed by a buffer provided by <see cref="IRazorBufferScope"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public class RazorBuffer : IHtmlContentBuilder
    {
        private readonly IRazorBufferScope _bufferScope;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of <see cref="RazorBuffer"/>.
        /// </summary>
        /// <param name="bufferScope">The <see cref="IRazorBufferScope"/>.</param>
        /// <param name="name">A name to identify this instance.</param>
        public RazorBuffer(IRazorBufferScope bufferScope, string name)
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
        public IList<RazorBufferSegment> BufferSegments { get; private set; }

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

            AppendValue(new RazorValue(unencoded));
            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Append(IHtmlContent content)
        {
            if (content == null)
            {
                return this;
            }

            AppendValue(new RazorValue(content));
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
            AppendValue(new RazorValue(value));
            return this;
        }

        private void AppendValue(RazorValue value)
        {
            RazorBufferSegment segment;
            if (BufferSegments == null)
            {
                BufferSegments = new List<RazorBufferSegment>(1);
                segment = _bufferScope.GetSegment();
                BufferSegments.Add(segment);
            }
            else
            {
                segment = BufferSegments[BufferSegments.Count - 1];
                if (CurrentCount == segment.Data.Count)
                {
                    segment = _bufferScope.GetSegment();
                    BufferSegments.Add(segment);
                    CurrentCount = 0;
                }
            }

            segment.Data.Array[segment.Data.Offset + CurrentCount] = value;
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
                var count = i == BufferSegments.Count - 1 ? CurrentCount : segment.Data.Count;

                for (var j = 0; j < count; j++)
                {
                    var value = segment.Data.Array[segment.Data.Offset + j];
                    value.WriteTo(writer, encoder);
                }
            }
        }

        private string DebuggerToString() => _name;
    }
}
