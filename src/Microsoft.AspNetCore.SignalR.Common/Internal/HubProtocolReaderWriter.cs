// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var buffer = _dataEncoder.Decode(input);
            return _hubProtocol.TryParseMessages(buffer, binder, out messages);
        }

        public byte[] WriteMessage(HubMessage hubMessage)
        {
            using (var ms = new MemoryStream())
            {
                _hubProtocol.WriteMessage(hubMessage, ms);
                return _dataEncoder.Encode(ms.ToArray());
            }
        }
    }
}
