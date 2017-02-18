// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public struct SendMessage
    {
        public MessageType Type { get; }
        public byte[] Payload { get; }
        public TaskCompletionSource<object> SendResult { get; }

        public SendMessage(byte[] payload, MessageType type, TaskCompletionSource<object> result)
        {
            Type = type;
            Payload = payload;
            SendResult = result;
        }
    }
}
