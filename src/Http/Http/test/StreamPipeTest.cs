// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace System.IO.Pipelines.Tests
{
    public abstract class StreamPipeTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;

        protected const int MinimumSegmentSize = 4096;

        public Stream Stream { get; set; }

        public PipeWriter Writer { get; set; }

        public PipeReader Reader { get; set; }

        public TestMemoryPool Pool { get; set; }

        protected StreamPipeTest()
        {
            Pool = new TestMemoryPool();
            Stream = new MemoryStream();
            Writer = new StreamPipeWriter(Stream, MinimumSegmentSize, Pool);
            Reader = new StreamPipeReader(Stream, new StreamPipeReaderOptions(MinimumSegmentSize, minimumReadThreshold: 256, Pool));
        }

        public void Dispose()
        {
            Writer.Complete();
            Reader.Complete();
            Pool.Dispose();
        }

        public byte[] Read()
        {
            Writer.FlushAsync().GetAwaiter().GetResult();
            return ReadWithoutFlush();
        }

        public string ReadAsString()
        {
            Writer.FlushAsync().GetAwaiter().GetResult();
            return Encoding.ASCII.GetString(ReadWithoutFlush());
        }

        public void Write(byte[] data)
        {
            Stream.Write(data, 0, data.Length);
            Stream.Position = 0;
        }

        public void WriteWithoutPosition(byte[] data)
        {
            Stream.Write(data, 0, data.Length);
        }

        public void Append(byte[] data)
        {
            var originalPosition = Stream.Position;
            Stream.Write(data, 0, data.Length);
            Stream.Position = originalPosition;
        }

        public byte[] ReadWithoutFlush()
        {
            Stream.Position = 0;
            var buffer = new byte[Stream.Length];
            var result = Stream.Read(buffer, 0, (int)Stream.Length);
            return buffer;
        }
    }
}
