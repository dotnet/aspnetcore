using System;
using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class MessageParserBenchmark
    {
        private static readonly Random Random = new Random();
        private byte[] _binaryInput;
        private byte[] _textInput;

        [Params(32, 64)]
        public int ChunkSize { get; set; }

        [Params(64, 128)]
        public int MessageLength { get; set; }

        [IterationSetup]
        public void Setup()
        {
            var buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            using (var writer = new MemoryBufferWriter())
            {
                BinaryMessageFormatter.WriteLengthPrefix(buffer.Length, writer);
                writer.Write(buffer);
                _binaryInput = writer.ToArray();
            }

            buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            using (var writer = new MemoryBufferWriter())
            {
                writer.Write(buffer);
                TextMessageFormatter.WriteRecordSeparator(writer);

                _textInput = writer.ToArray();
            }
        }

        [Benchmark]
        public void SingleBinaryMessage()
        {
            var data = new ReadOnlySequence<byte>(_binaryInput);
            if (!BinaryMessageParser.TryParseMessage(ref data, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }

        [Benchmark]
        public void SingleTextMessage()
        {
            var data = new ReadOnlySequence<byte>(_textInput);
            if (!TextMessageParser.TryParseMessage(ref data, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}
