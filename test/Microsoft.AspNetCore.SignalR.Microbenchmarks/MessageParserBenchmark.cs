using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.AspNetCore.Sockets.Tests.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    [Config(typeof(CoreConfig))]
    public class MessageParserBenchmark
    {
        private static readonly Random Random = new Random();
        private readonly TextMessageParser _textMessageParser = new TextMessageParser();
        private readonly BinaryMessageParser _binaryMessageParser = new BinaryMessageParser();
        private ReadOnlyBytes _binaryInput;
        private ReadOnlyBytes _textInput;

        [Params(32, 64)]
        public int ChunkSize { get; set; }

        [Params(64, 128)]
        public int MessageLength { get; set; }

        [Setup]
        public void Setup()
        {
            var buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            var output = new ArrayOutput(MessageLength + 32);
            if (!BinaryMessageFormatter.TryWriteMessage(buffer, output))
            {
                throw new InvalidOperationException("Failed to format message");
            }

            _binaryInput = output.ToArray().ToChunkedReadOnlyBytes(ChunkSize);

            buffer = new byte[MessageLength];
            Random.NextBytes(buffer);
            output = new ArrayOutput(MessageLength + 32);
            if (!TextMessageFormatter.TryWriteMessage(buffer, output))
            {
                throw new InvalidOperationException("Failed to format message");
            }

            _textInput = output.ToArray().ToChunkedReadOnlyBytes(ChunkSize);
        }

        [Benchmark]
        public void SingleBinaryMessage()
        {
            var reader = new BytesReader(_binaryInput);
            if (!_binaryMessageParser.TryParseMessage(ref reader, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }

        [Benchmark]
        public void SingleTextMessage()
        {
            var reader = new BytesReader(_textInput);
            if (!_textMessageParser.TryParseMessage(ref reader, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}