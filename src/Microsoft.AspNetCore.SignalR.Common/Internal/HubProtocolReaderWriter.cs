// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class HubProtocolReaderWriter
    {
        private readonly IHubProtocol _hubProtocol;
        private readonly IDataEncoder _dataEncoder;

        public HubProtocolReaderWriter(IHubProtocol hubProtocol, IDataEncoder dataEncoder)
        {
            _hubProtocol = hubProtocol;
            _dataEncoder = dataEncoder;
        }

        public bool ReadMessages(ReadOnlyBuffer<byte> buffer, IInvocationBinder binder, out IList<HubMessage> messages, out SequencePosition consumed, out SequencePosition examined)
        {
            // TODO: Fix this implementation to be incremental
            consumed = buffer.End;
            examined = consumed;

            return ReadMessages(buffer.ToArray(), binder, out messages);
        }

        public bool ReadMessages(byte[] input, IInvocationBinder binder, out IList<HubMessage> messages)
        {
            messages = new List<HubMessage>();
            ReadOnlySpan<byte> span = input;
            while (span.Length > 0 && _dataEncoder.TryDecode(ref span, out var data))
            {
                _hubProtocol.TryParseMessages(data, binder, messages);
            }
            return messages.Count > 0;
        }

        public byte[] WriteMessage(HubMessage hubMessage)
        {
            using (var ms = new MemoryStream())
            {
                _hubProtocol.WriteMessage(hubMessage, ms);
                return _dataEncoder.Encode(ms.ToArray());
            }
        }

        public override bool Equals(object obj)
        {
            var readerWriter = obj as HubProtocolReaderWriter;
            if (readerWriter == null)
            {
                return false;
            }

            // Note: ReferenceEquals on HubProtocol works for our implementation of IHubProtocolResolver because we use Singletons from DI
            // However if someone replaces the implementation and returns a new ProtocolResolver for every connection they wont get the perf benefits
            // Memory growth is mitigated by capping the cache size
            return ReferenceEquals(_dataEncoder, readerWriter._dataEncoder) && ReferenceEquals(_hubProtocol, readerWriter._hubProtocol);
        }

        // This should never be used, needed because you can't override Equals without it
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
