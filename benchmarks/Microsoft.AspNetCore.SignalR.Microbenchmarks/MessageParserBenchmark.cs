using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    [Config(typeof(CoreConfig))]
    public class MessageParserBenchmark
    {
        private static readonly Random Random = new Random();
        private ReadOnlyMemory<byte> _binaryInput;
        private ReadOnlyMemory<byte> _textInput;

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
            BinaryMessageFormatter.WriteMessage(buffer, output);

            _binaryInput = output.ToArray();

            buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            output = new MemoryStream();
            TextMessageFormatter.WriteMessage(buffer, output);

            _textInput = output.ToArray();
        }

        [Benchmark]
        public void SingleBinaryMessage()
        {
            var buffer = _binaryInput;
            if (!BinaryMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }

        [Benchmark]
        public void SingleTextMessage()
        {
            var buffer = _textInput;
            if (!TextMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}
