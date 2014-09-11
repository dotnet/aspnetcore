// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A <see cref="TextWriter"/> that represents individual write operations as a sequence of strings when buffering.
    /// The writer is backed by an unbuffered writer. When <c>Flush</c> or <c>FlushAsync</c> is invoked, the writer
    /// copies all content to the unbuffered writier and switches to writing to the unbuffered writer for all further
    /// write operations.
    /// </summary>
    /// <remarks>
    /// This is primarily designed to avoid creating large in-memory strings.
    /// Refer to https://aspnetwebstack.codeplex.com/workitem/585 for more details.
    /// </remarks>
    public class RazorTextWriter : TextWriter, IBufferedTextWriter
    {
        private static readonly Task _completedTask = Task.FromResult(0);
        private readonly TextWriter _unbufferedWriter;
        private readonly Encoding _encoding;

        /// <summary>
        /// Creates a new instance of <see cref="RazorTextWriter"/>.
        /// </summary>
        /// <param name="unbufferedWriter">The <see cref="TextWriter"/> to write output to when this instance
        /// is no longer buffering.</param>
        /// <param name="encoding">The character <see cref="Encoding"/> in which the output is written.</param>
        public RazorTextWriter(TextWriter unbufferedWriter, Encoding encoding)
        {
            _unbufferedWriter = unbufferedWriter;
            _encoding = encoding;
            Buffer = new BufferEntryCollection();
        }

        /// <inheritdoc />
        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        /// <inheritdoc />
        public bool IsBuffering { get; private set; } = true;

        /// <summary>
        /// A collection of entries buffered by this instance of <see cref="RazorTextWriter"/>.
        /// </summary>
        public BufferEntryCollection Buffer { get; private set; }

        /// <inheritdoc />
        public override void Write(char value)
        {
            if (IsBuffering)
            {
                Buffer.Add(value.ToString());
            }
            else
            {
                _unbufferedWriter.Write(value);
            }
        }

        /// <inheritdoc />
        public override void Write([NotNull] char[] buffer, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (IsBuffering)
            {
                Buffer.Add(buffer, index, count);
            }
            else
            {
                _unbufferedWriter.Write(buffer, index, count);
            }
        }

        /// <inheritdoc />
        public override void Write(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (IsBuffering)
                {
                    Buffer.Add(value);
                }
                else
                {
                    _unbufferedWriter.Write(value);
                }
            }
        }

        /// <inheritdoc />
        public override Task WriteAsync(char value)
        {
            if (IsBuffering)
            {
                Write(value);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteAsync(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteAsync([NotNull] char[] buffer, int index, int count)
        {
            if (IsBuffering)
            {
                Write(buffer, index, count);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteAsync(buffer, index, count);
            }

        }

        /// <inheritdoc />
        public override Task WriteAsync(string value)
        {
            if (IsBuffering)
            {
                Write(value);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteAsync(value);
            }
        }

        /// <inheritdoc />
        public override void WriteLine()
        {
            if (IsBuffering)
            {
                Buffer.Add(Environment.NewLine);
            }
            else
            {
                _unbufferedWriter.WriteLine();
            }
        }

        /// <inheritdoc />
        public override void WriteLine(string value)
        {
            if (IsBuffering)
            {
                Write(value);
                WriteLine();
            }
            else
            {
                _unbufferedWriter.WriteLine(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char value)
        {
            if (IsBuffering)
            {
                WriteLine(value);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteLineAsync(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(char[] value, int start, int offset)
        {
            if (IsBuffering)
            {
                WriteLine(value, start, offset);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteLineAsync(value, start, offset);
            }
        }

        /// <inheritdoc />
        public override Task WriteLineAsync(string value)
        {
            if (IsBuffering)
            {
                WriteLine(value);
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteLineAsync(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteLineAsync()
        {
            if (IsBuffering)
            {
                WriteLine();
                return _completedTask;
            }
            else
            {
                return _unbufferedWriter.WriteLineAsync();
            }
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
                CopyTo(_unbufferedWriter);
            }

            _unbufferedWriter.Flush();
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
                await CopyToAsync(_unbufferedWriter);
            }

            await _unbufferedWriter.FlushAsync();
        }

        /// <inheritdoc />
        public void CopyTo(TextWriter writer)
        {
            var targetRazorTextWriter = writer as RazorTextWriter;
            if (targetRazorTextWriter != null && targetRazorTextWriter.IsBuffering)
            {
                targetRazorTextWriter.Buffer.Add(Buffer);
            }
            else
            {
                // If the target writer is not buffering, we can directly copy to it's unbuffered writer
                var targetWriter = targetRazorTextWriter != null ? targetRazorTextWriter._unbufferedWriter : writer;
                WriteList(targetWriter, Buffer);
            }
        }

        /// <inheritdoc />
        public Task CopyToAsync(TextWriter writer)
        {
            var targetRazorTextWriter = writer as RazorTextWriter;
            if (targetRazorTextWriter != null && targetRazorTextWriter.IsBuffering)
            {
                targetRazorTextWriter.Buffer.Add(Buffer);
            }
            else
            {
                // If the target writer is not buffering, we can directly copy to it's unbuffered writer
                var targetWriter = targetRazorTextWriter != null ? targetRazorTextWriter._unbufferedWriter : writer;
                return WriteListAsync(targetWriter, Buffer);
            }

            return _completedTask;
        }

        private static void WriteList(TextWriter writer, BufferEntryCollection values)
        {
            foreach (var value in values)
            {
                writer.Write(value);
            }
        }

        private static async Task WriteListAsync(TextWriter writer, BufferEntryCollection values)
        {
            foreach (var value in values)
            {
                await writer.WriteAsync(value);
            }
        }
    }
}