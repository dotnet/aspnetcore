// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Tests
{
    public abstract class PipeTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;

        protected const int MinimumSegmentSize = 4096;

        public MemoryStream MemoryStream { get; set; }

        public PipeWriter Writer { get; set; }

        public PipeReader Reader { get; set; }

        protected PipeTest()
        {
            MemoryStream = new MemoryStream();
            Writer = new StreamPipeWriter(MemoryStream, MinimumSegmentSize, new TestMemoryPool());
            Reader = new StreamPipeReader(MemoryStream, new StreamPipeReaderOptions(MinimumSegmentSize, minimumReadThreshold: 256, new TestMemoryPool()));
        }

        public void Dispose()
        {
            Writer.Complete();
            Reader.Complete();
        }

        public byte[] Read()
        {
            Writer.FlushAsync().GetAwaiter().GetResult();
            return ReadWithoutFlush();
        }

        public void Write(byte[] data)
        {
            MemoryStream.Write(data, 0, data.Length);
            MemoryStream.Position = 0;
        }

        public void WriteWithoutPosition(byte[] data)
        {
            MemoryStream.Write(data, 0, data.Length);
        }

        public void Append(byte[] data)
        {
            var originalPosition = MemoryStream.Position;
            MemoryStream.Write(data, 0, data.Length);
            MemoryStream.Position = originalPosition;
        }

        public byte[] ReadWithoutFlush()
        {
            MemoryStream.Position = 0;
            var buffer = new byte[MemoryStream.Length];
            var result = MemoryStream.Read(buffer, 0, (int)MemoryStream.Length);
            return buffer;
        }
    }
}
