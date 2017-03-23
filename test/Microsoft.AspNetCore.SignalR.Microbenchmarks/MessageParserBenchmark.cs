using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.AspNetCore.Sockets.Tests.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    [Config(typeof(CoreConfig))]
    public class MessageParserBenchmark
    {
        private static readonly Random Random = new Random();
        private readonly MessageParser _parser = new MessageParser();
        private ReadOnlyBytes _input;
        private byte[] _buffer;

        [Params(32, 64)]
        public int ChunkSize { get; set; }

        [Params(64, 128)]
        public int MessageLength { get; set; }

        [Params(MessageFormat.Text, MessageFormat.Binary)]
        public MessageFormat Format { get; set; }

        [Setup]
        public void Setup()
        {
            _buffer = new byte[MessageLength];
            Random.NextBytes(_buffer);
            var message = new Message(_buffer, MessageType.Binary);
            var output = new ArrayOutput(MessageLength + 32);
            if (!MessageFormatter.TryWriteMessage(message, output, Format))
            {
                throw new InvalidOperationException("Failed to format message");
            }
            _input = output.ToArray().ToChunkedReadOnlyBytes(ChunkSize);
        }

        [Benchmark]
        public void SingleBinaryMessage()
        {
            var reader = new BytesReader(_input);
            if (!_parser.TryParseMessage(ref reader, Format, out _))
            {
                throw new InvalidOperationException("Failed to parse");
            }
        }
    }
}