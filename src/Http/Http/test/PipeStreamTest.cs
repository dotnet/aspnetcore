// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests
{
    public class PipeStreamTest : IDisposable
    {
        public Stream ReadingStream { get; set; }
        public Stream WritingStream { get; set; }

        public Pipe Pipe { get; set; }
        public PipeReader Reader => Pipe.Reader;

        public PipeWriter Writer => Pipe.Writer;
        public PipeStreamTest()
        {
            Pipe = new Pipe();
            ReadingStream = new ReadOnlyPipeStream(Reader);
            WritingStream = new WriteOnlyPipeStream(Writer);
        }

        public void Dispose()
        {
            Writer.Complete();
            Reader.Complete();
        }

        public async Task WriteStringToStreamAsync(string input)
        {
            await WritingStream.WriteAsync(Encoding.ASCII.GetBytes(input));
        }

        public async Task<string> ReadFromPipeAsStringAsync()
        {
            var readResult = await Reader.ReadAsync();
            var result = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            Reader.AdvanceTo(readResult.Buffer.End);
            return result;
        }

        public async Task<byte[]> ReadFromPipeAsByteArrayAsync()
        {
            var readResult = await Reader.ReadAsync();
            var result = readResult.Buffer.ToArray();
            Reader.AdvanceTo(readResult.Buffer.End);
            return result;
        }
    }
}
