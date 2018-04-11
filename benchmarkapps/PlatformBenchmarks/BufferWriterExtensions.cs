// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers.Text;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal static class BufferWriterExtensions
    {
        private const int MaxULongByteLength = 20;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteNumeric(ref this BufferWriter<PipeWriter> buffer, ulong number)
        {
            // Try to format directly
            if (Utf8Formatter.TryFormat(number, buffer.Span, out int bytesWritten))
            {
                buffer.Advance(bytesWritten);
            }
            else
            {
                // Ask for at least 20 bytes
                buffer.Ensure(MaxULongByteLength);

                Debug.Assert(buffer.Span.Length >= 20, "Buffer is < 20 bytes");

                // Try again
                if (Utf8Formatter.TryFormat(number, buffer.Span, out bytesWritten))
                {
                    buffer.Advance(bytesWritten);
                }
            }
        }
    }
}