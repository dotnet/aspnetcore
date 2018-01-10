// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class HubProtocolBenchmark
    {
        private HubProtocolReaderWriter _hubProtocolReaderWriter;
        private byte[] _binaryInput;
        private TestBinder _binder;
        private HubMessage _hubMessage;

        [Params(Message.NoArguments, Message.FewArguments, Message.ManyArguments, Message.LargeArguments)]
        public Message Input { get; set; }

        [Params(Protocol.MsgPack, Protocol.Json)]
        public Protocol HubProtocol { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            switch (HubProtocol)
            {
                case Protocol.MsgPack:
                    _hubProtocolReaderWriter = new HubProtocolReaderWriter(new MessagePackHubProtocol(), new PassThroughEncoder());
                    break;
                case Protocol.Json:
                    _hubProtocolReaderWriter = new HubProtocolReaderWriter(new JsonHubProtocol(), new PassThroughEncoder());
                    break;
            }

            switch (Input)
            {
                case Message.NoArguments:
                    _hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null);
                    break;
                case Message.FewArguments:
                    _hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, 1, "Foo", 2.0f);
                    break;
                case Message.ManyArguments:
                    _hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, 1, "string", 2.0f, true, (byte)9, new byte[] { 5, 4, 3, 2, 1 }, 'c', 123456789101112L);
                    break;
                case Message.LargeArguments:
                    _hubMessage = new InvocationMessage(target: "Target", argumentBindingException: null, new string('F', 10240), new byte[10240]);
                    break;
            }

            _binaryInput = GetBytes(_hubMessage);
            _binder = new TestBinder(_hubMessage);
        }

        [Benchmark]
        public void ReadSingleMessage()
        {
            if (!_hubProtocolReaderWriter.ReadMessages(_binaryInput, _binder, out var _))
            {
                throw new InvalidOperationException("Failed to read message");
            }
        }

        [Benchmark]
        public void WriteSingleMessage()
        {
            if (_hubProtocolReaderWriter.WriteMessage(_hubMessage).Length != _binaryInput.Length)
            {
                throw new InvalidOperationException("Failed to write message");
            }
        }

        public enum Protocol
        {
            MsgPack = 0,
            Json = 1
        }

        public enum Message
        {
            NoArguments = 0,
            FewArguments = 1,
            ManyArguments = 2,
            LargeArguments = 3
        }

        private byte[] GetBytes(HubMessage hubMessage)
        {
            return _hubProtocolReaderWriter.WriteMessage(_hubMessage);
        }
    }
}
