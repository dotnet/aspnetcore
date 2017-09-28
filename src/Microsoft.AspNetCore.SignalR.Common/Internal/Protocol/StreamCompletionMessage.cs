// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class StreamCompletionMessage : HubMessage
    {
        public string Error { get; }
        public StreamCompletionMessage(string invocationId, string error)
            : base(invocationId)
        {
            Error = error;
        }

        public override string ToString()
        {
            var errorStr = Error == null ? "<<null>>" : $"\"{Error}\"";
            return $"StreamCompletion {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Error)}: {errorStr} }}";
        }

    }
}
