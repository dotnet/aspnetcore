// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public static class HubProtocolConstants
    {
        public const int InvocationMessageType = 1;
        public const int StreamItemMessageType = 2;
        public const int CompletionMessageType = 3;
        public const int StreamInvocationMessageType = 4;
        public const int CancelInvocationMessageType = 5;
        public const int PingMessageType = 6;
        public const int CloseMessageType = 7;
    }
}
