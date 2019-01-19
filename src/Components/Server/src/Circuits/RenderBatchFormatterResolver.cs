// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Components.Rendering;
using System;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// A MessagePack IFormatterResolver that provides an efficient binary serialization
    /// of <see cref="RenderBatch"/>. The client-side code knows how to walk through this
    /// binary representation directly, without it first being parsed as an object graph.
    /// </summary>
    internal class RenderBatchFormatterResolver : IFormatterResolver
    {
        public static readonly RenderBatchFormatterResolver Instance = new RenderBatchFormatterResolver();

        public IMessagePackFormatter<T> GetFormatter<T>()
            => typeof(T) == typeof(RenderBatch) ? (IMessagePackFormatter<T>)RenderBatchFormatter.Instance : null;

        private class RenderBatchFormatter : IMessagePackFormatter<RenderBatch>
        {
            public static readonly RenderBatchFormatter Instance = new RenderBatchFormatter();

            // No need to accept incoming RenderBatch
            public RenderBatch Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
                => throw new NotImplementedException();

            public int Serialize(ref byte[] bytes, int offset, RenderBatch value, IFormatterResolver formatterResolver)
            {
                // Instead of using MessagePackBinary.WriteBytes, we write into a stream that
                // knows how to write the data using MessagePack writer APIs. The benefit
                // is that we don't have to allocate a second large buffer to capture the
                // RenderBatchWriter output - we can just write directly to the underlying
                // output buffer.
                using (var bufferStream = new MessagePackBufferStream(bytes, offset))
                using (var renderBatchWriter = new RenderBatchWriter(bufferStream, leaveOpen: false))
                {
                    renderBatchWriter.Write(value);

                    bytes = bufferStream.Buffer; // In case the buffer was expanded
                    return (int)bufferStream.Length;
                }
            }
        }
    }
}
