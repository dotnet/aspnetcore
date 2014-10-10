// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A <see cref="TextWriter"/> that is backed by a unbuffered writer (over the Response stream) and a buffered
    /// <see cref="StringCollectionTextWriter"/>. When <c>Flush</c> or <c>FlushAsync</c> is invoked, the writer
    /// copies all content from the buffered writer to the unbuffered one and switches to writing to the unbuffered
    /// writer for all further write operations.
    /// </summary>
    /// <remarks>
    /// This type is designed to avoid creating large in-memory strings when buffering and supporting the contract that
    /// <see cref="RazorPage.FlushAsync"/> expects.
    /// </remarks>
    public class RazorTextWriter : TextWriter, IBufferedTextWriter
    {
        /// <summary>
        /// Creates a new instance of <see cref="RazorTextWriter"/>.
        /// </summary>
        /// <param name="unbufferedWriter">The <see cref="TextWriter"/> to write output to when this instance
        /// is no longer buffering.</param>
        /// <param name="encoding">The character <see cref="Encoding"/> in which the output is written.</param>
        public RazorTextWriter(TextWriter unbufferedWriter, Encoding encoding)
        {
            UnbufferedWriter = unbufferedWriter;
            BufferedWriter = new StringCollectionTextWriter(encoding);
            TargetWriter = BufferedWriter;
        }

        /// <inheritdoc />
        public override Encoding Encoding
        {
            get { return BufferedWriter.Encoding; }
        }

        /// <inheritdoc />
        public bool IsBuffering { get; private set; } = true;

        // Internal for unit testing
        internal StringCollectionTextWriter BufferedWriter { get; }

        private TextWriter UnbufferedWriter { get; }

        private TextWriter TargetWriter { get; set; }

        /// <inheritdoc />
        public override void Write(char value)
        {
            TargetWriter.Write(value);
        }

        /// <inheritdoc />
        public override void Write(object value)
        {
            var htmlString = value as HtmlString;
            if (htmlString != null)
            {
                htmlString.WriteTo(TargetWriter);
                return;
            }

            base.Write(value);
        }

        /// <inheritdoc />
        public override void Write([NotNull] char[] buffer, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0 || (buffer.Length - index < count))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            TargetWriter.Write(buffer, index, count);
        }

        /// <inheritdoc />
        public override void Write(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                TargetWriter.Write(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteAsync(char value)
        {
            return TargetWriter.WriteAsync(value);
        }

        /// <inheritdoc />
        public override Task WriteAsync([NotNull] char[] buffer, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0 || (buffer.Length - index < count))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return TargetWriter.WriteAsync(buffer, index, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(string value)
        {
            return TargetWriter.WriteAsync(value);
        }

        /// <inheritdoc />
        public override void WriteLine()
        {
            TargetWriter.WriteLine();
        }

        /// <inheritdoc />
        public override void WriteLine(string value)
        {
            TargetWriter.WriteLine(value);
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char value)
        {
            return TargetWriter.WriteLineAsync(value);
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char[] value, int start, int offset)
        {
            return TargetWriter.WriteLineAsync(value, start, offset);
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(string value)
        {
            return TargetWriter.WriteLineAsync(value);
        }

        /// <inheritdoc />
        public override Task WriteLineAsync()
        {
            return TargetWriter.WriteLineAsync();
        }

        /// <summary>
        /// Copies the buffered content to the unbuffered writer and invokes flush on it.
        /// Additionally causes this instance to no longer buffer and direct all write operations
        /// to the unbuffered writer.
        /// </summary>
        public override void Flush()
        {
            if (IsBuffering)
            {
                IsBuffering = false;
                TargetWriter = UnbufferedWriter;
                CopyTo(UnbufferedWriter);
            }

            UnbufferedWriter.Flush();
        }

        /// <summary>
        /// Copies the buffered content to the unbuffered writer and invokes flush on it.
        /// Additionally causes this instance to no longer buffer and direct all write operations
        /// to the unbuffered writer.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous copy and flush operations.</returns>
        public override async Task FlushAsync()
        {
            if (IsBuffering)
            {
                IsBuffering = false;
                TargetWriter = UnbufferedWriter;
                await CopyToAsync(UnbufferedWriter);
            }

            await UnbufferedWriter.FlushAsync();
        }

        /// <inheritdoc />
        public void CopyTo(TextWriter writer)
        {
            writer = UnWrapRazorTextWriter(writer);
            BufferedWriter.CopyTo(writer);
        }

        /// <inheritdoc />
        public Task CopyToAsync(TextWriter writer)
        {
            writer = UnWrapRazorTextWriter(writer);
            return BufferedWriter.CopyToAsync(writer);
        }

        private static TextWriter UnWrapRazorTextWriter(TextWriter writer)
        {
            var targetRazorTextWriter = writer as RazorTextWriter;
            if (targetRazorTextWriter != null)
            {
                writer = targetRazorTextWriter.IsBuffering ? targetRazorTextWriter.BufferedWriter :
                                                             targetRazorTextWriter.UnbufferedWriter;
            }

            return writer;
        }
    }
}