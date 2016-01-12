// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
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

            Pages = new List<ViewBufferPage>();
        }

        /// <summary>
        /// Gets the backing buffer.
        /// </summary>
        public IList<ViewBufferPage> Pages { get; }

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

            // Perf: special case ViewBuffers so we can 'combine' them.
            var otherBuffer = content as ViewBuffer;
            if (otherBuffer == null)
            {
                AppendValue(new ViewBufferValue(content));
                return this;
            }

            for (var i = 0; i < otherBuffer.Pages.Count; i++)
            {
                var otherPage = otherBuffer.Pages[i];
                var currentPage = Pages.Count == 0 ? null : Pages[Pages.Count - 1];

                // If the other page is less or equal to than half full, let's copy it's to the current page if
                // possible.
                var isLessThanHalfFull = 2 * otherPage.Count <= otherPage.Capacity;
                if (isLessThanHalfFull &&
                    currentPage != null &&
                    currentPage.Capacity - currentPage.Count >= otherPage.Count)
                {
                    // We have room, let's copy the items.
                    Array.Copy(
                        sourceArray: otherPage.Buffer,
                        sourceIndex: 0,
                        destinationArray: currentPage.Buffer,
                        destinationIndex: currentPage.Count,
                        length: otherPage.Count);

                    currentPage.Count += otherPage.Count;

                    // Now we can return this page, and it can be reused in the scope of this request.
                    _bufferScope.ReturnSegment(otherPage.Buffer);
                }
                else
                {
                    // Otherwise, let's just take the the page from the other buffer.
                    Pages.Add(otherPage);
                }

            }

            otherBuffer.Clear();
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
            var page = GetCurrentPage();
            page.Append(value);
        }

        private ViewBufferPage GetCurrentPage()
        {
            ViewBufferPage page;
            if (Pages.Count == 0)
            {
                page = new ViewBufferPage(_bufferScope.GetSegment());
                Pages.Add(page);
            }
            else
            {
                page = Pages[Pages.Count - 1];
                if (page.IsFull)
                {
                    page = new ViewBufferPage(_bufferScope.GetSegment());
                    Pages.Add(page);
                }
            }

            return page;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Clear()
        {
            if (Pages != null)
            {
                Pages.Clear();
            }

            return this;
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (Pages == null)
            {
                return;
            }

            var htmlTextWriter = writer as HtmlTextWriter;
            if (htmlTextWriter != null)
            {
                htmlTextWriter.Write(this);
                return;
            }

            for (var i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                for (var j = 0; j < page.Count; j++)
                {
                    var value = page.Buffer[j];

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
            if (Pages == null)
            {
                return;
            }

            var htmlTextWriter = writer as HtmlTextWriter;
            if (htmlTextWriter != null)
            {
                htmlTextWriter.Write(this);
                return;
            }

            for (var i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                for (var j = 0; j < page.Count; j++)
                {
                    var value = page.Buffer[j];

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
                        await writer.FlushAsync();
                        continue;
                    }
                }
            }
        }

        private string DebuggerToString() => _name;
    }
}
