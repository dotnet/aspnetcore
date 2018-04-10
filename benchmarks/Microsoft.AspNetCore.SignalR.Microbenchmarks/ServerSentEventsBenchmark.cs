using System;
using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class ServerSentEventsBenchmark
    {
        private ServerSentEventsMessageParser _parser;
        private byte[] _sseFormattedData;
        private byte[] _rawData;

        [Params(Message.NoArguments, Message.FewArguments, Message.ManyArguments, Message.LargeArguments)]
        public Message Input { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var hubProtocol = new JsonHubProtocol();
            HubMessage hubMessage = null;
            switch (Input)
            {
                case Message.NoArguments:
                    hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null);
                    break;
                case Message.FewArguments:
                    hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, 1, "Foo", 2.0f);
                    break;
                case Message.ManyArguments:
                    hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, 1, "string", 2.0f, true, (byte)9, new[] { 5, 4, 3, 2, 1 }, 'c', 123456789101112L);
                    break;
                case Message.LargeArguments:
                    hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, new string('F', 10240), new string('B', 10240));
                    break;
            }

            _parser = new ServerSentEventsMessageParser();
            _rawData = hubProtocol.GetMessageBytes(hubMessage);
            var ms = new MemoryStream();
            ServerSentEventsMessageFormatter.WriteMessage(_rawData, ms);
            _sseFormattedData = ms.ToArray();
        }

        [Benchmark]
        public void ReadSingleMessage()
        {
            var buffer = new ReadOnlySequence<byte>(_sseFormattedData);

            if (_parser.ParseMessage(buffer, out _, out _, out _) != ServerSentEventsMessageParser.ParseResult.Completed)
            {
                throw new InvalidOperationException("Parse failed!");
            }

            _parser.Reset();
        }

        [Benchmark]
        public void WriteSingleMessage()
        {
            ServerSentEventsMessageFormatter.WriteMessage(_rawData, Stream.Null);
        }

        public enum Message
        {
            NoArguments = 0,
            FewArguments = 1,
            ManyArguments = 2,
            LargeArguments = 3
        }
    }
}
