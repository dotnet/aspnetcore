using System;
using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    [Config(typeof(CoreConfig))]
    public class MessageParserBenchmark
    {
        private static readonly Random Random = new Random();
        private readonly TextMessageParser _textMessageParser = new TextMessageParser();
        private readonly BinaryMessageParser _binaryMessageParser = new BinaryMessageParser();
        private ReadOnlyBuffer<byte> _binaryInput;
        private ReadOnlyBuffer<byte> _textInput;

        [Params(32, 64)]
        public int ChunkSize { get; set; }

        [Params(64, 128)]
        public int MessageLength { get; set; }

        [Setup]
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
            var buffer = _binaryInput.Span;
            if (!_binaryMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }

        [Benchmark]
        public void SingleTextMessage()
        {
            var buffer = _textInput.Span;
            if (!_textMessageParser.TryParseMessage(ref buffer, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}