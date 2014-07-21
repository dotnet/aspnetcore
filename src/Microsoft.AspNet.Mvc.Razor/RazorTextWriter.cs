// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A <see cref="TextWriter"/> that represents individual write operations as a sequence of strings.
    /// </summary>
    /// <remarks>
    /// This is primarily designed to avoid creating large in-memory strings.
    /// Refer to https://aspnetwebstack.codeplex.com/workitem/585 for more details.
    /// </remarks>
    public class RazorTextWriter : TextWriter
    {
        private static readonly Task _completedTask = Task.FromResult(0);
        private readonly Encoding _encoding;

        public RazorTextWriter(Encoding encoding)
        {
            _encoding = encoding;
            Buffer = new BufferEntryCollection();
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public BufferEntryCollection Buffer { get; private set; }

        /// <inheritdoc />
        public override void Write(char value)
        {
            Buffer.Add(value.ToString());
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

            Buffer.Add(buffer, index, count);
        }

        /// <inheritdoc />
        public override void Write(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Buffer.Add(value);
            }
        }

        /// <inheritdoc />
        public override Task WriteAsync(char value)
        {
            Write(value);
            return _completedTask;
        }

        /// <inheritdoc />
        public override Task WriteAsync([NotNull] char[] buffer, int index, int count)
        {
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
            Buffer.Add(Environment.NewLine);
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
        /// Copies the content of the <see cref="RazorTextWriter"/> to the <see cref="TextWriter"/> instance.
        /// </summary>
        /// <param name="writer">The writer to copy contents to.</param>
        public void CopyTo(TextWriter writer)
        {
            var targetRazorTextWriter = writer as RazorTextWriter;
            if (targetRazorTextWriter != null)
            {
                targetRazorTextWriter.Buffer.Add(Buffer);
            }
            else
            {
                WriteList(writer, Buffer);
            }
        }

        /// <summary>
        /// Copies the content of the <see cref="RazorTextWriter"/> to the specified <see cref="TextWriter"/> instance.
        /// </summary>
        /// <param name="writer">The writer to copy contents to.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public Task CopyToAsync(TextWriter writer)
        {
            var targetRazorTextWriter = writer as RazorTextWriter;
            if (targetRazorTextWriter != null)
            {
                targetRazorTextWriter.Buffer.Add(Buffer);
            }
            else
            {
                return WriteListAsync(writer, Buffer);
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