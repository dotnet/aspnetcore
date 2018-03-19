using System;
using System.IO;
using BenchmarkDotNet.Attributes;
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
            var output = new MemoryStream();
            BinaryMessageFormatter.WriteLengthPrefix(buffer.Length, output);
            output.Write(buffer, 0, buffer.Length);

            _binaryInput = output.ToArray();

            buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            output = new MemoryStream();
            output.Write(buffer, 0, buffer.Length);
            TextMessageFormatter.WriteRecordSeparator(output);

            _textInput = output.ToArray();
        }

        [Benchmark]
        public void SingleBinaryMessage()
        {
            ReadOnlyMemory<byte> buffer = _binaryInput;
            if (!BinaryMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }

        [Benchmark]
        public void SingleTextMessage()
        {
            ReadOnlyMemory<byte> buffer = _textInput;
            if (!TextMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}
