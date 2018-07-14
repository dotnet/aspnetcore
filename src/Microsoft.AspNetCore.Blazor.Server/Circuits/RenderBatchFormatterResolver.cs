// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Blazor.Rendering;
using System;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    /// <summary>
    /// A MessagePack IFormatterResolver that provides an efficient binary serialization
    /// of <see cref="RenderBatch"/>. The client-side code knows how to walk through this
    /// binary representation directly, without it first being parsed as an object graph.
    /// </summary>
    internal class RenderBatchFormatterResolver : IFormatterResolver
    {
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
                using (var memoryStream = new MemoryStream())
                using (var renderBatchWriter = new RenderBatchWriter(memoryStream, leaveOpen: false))
                {
                    renderBatchWriter.Write(value);
                    
                    var bytesBuffer = memoryStream.GetBuffer();
                    return MessagePackBinary.WriteBytes(ref bytes, offset, bytesBuffer, 0, (int)memoryStream.Length);
                }
                    
            }
        }
    }
}
