using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class ServerSentEventsBenchmark
    {
        private ServerSentEventsMessageParser _parser;
        private byte[] _sseFormattedData;
        private ReadOnlySequence<byte> _rawData;

        [Params(Message.NoArguments, Message.FewArguments, Message.ManyArguments, Message.LargeArguments)]
        public Message Input { get; set; }

        [Params("json", "json-formatted")]
        public string Protocol { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            IHubProtocol protocol;

            if (Protocol == "json")
            {
                protocol = new NewtonsoftJsonHubProtocol();
            }
            else
            {
                // New line in result to trigger SSE formatting
                protocol = new NewtonsoftJsonHubProtocol
                {
                    PayloadSerializer = { Formatting = Formatting.Indented }
                };
            }

            HubMessage hubMessage = null;
            switch (Input)
            {
                case Message.NoArguments:
                    hubMessage = new InvocationMessage("Target", Array.Empty<object>());
                    break;
                case Message.FewArguments:
                    hubMessage = new InvocationMessage("Target", new object[] { 1, "Foo", 2.0f });
                    break;
                case Message.ManyArguments:
                    hubMessage = new InvocationMessage("Target", new object[] { 1, "string", 2.0f, true, (byte)9, new[] { 5, 4, 3, 2, 1 }, 'c', 123456789101112L });
                    break;
                case Message.LargeArguments:
                    hubMessage = new InvocationMessage("Target", new object[] { new string('F', 10240), new string('B', 10240) });
                    break;
            }

            _parser = new ServerSentEventsMessageParser();
            _rawData = new ReadOnlySequence<byte>(protocol.GetMessageBytes(hubMessage));
            var ms = new MemoryStream();
            ServerSentEventsMessageFormatter.WriteMessageAsync(_rawData, ms, default).GetAwaiter().GetResult();
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
        public Task WriteSingleMessage()
        {
            return ServerSentEventsMessageFormatter.WriteMessageAsync(_rawData, Stream.Null, default);
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
