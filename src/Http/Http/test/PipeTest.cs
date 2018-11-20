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

        public MemoryStream MemoryStream { get; set; }

        public PipeWriter Writer { get; set; }

        protected PipeTest()
        {
            MemoryStream = new MemoryStream();
            Writer = new StreamPipeWriter(MemoryStream, 4096, new TestMemoryPool());
        }

        public void Dispose()
        {
            Writer.Complete();
        }

        public byte[] Read()
        {
            Writer.FlushAsync().GetAwaiter().GetResult();
            return ReadWithoutFlush();
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
