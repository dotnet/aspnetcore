// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class InvocationMessage : HubMessage
    {
        public string Target { get; }

        public object[] Arguments { get; }

        public bool NonBlocking { get; }

        public InvocationMessage(string invocationId, bool nonBlocking, string target, params object[] arguments) : base(invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Target = target;
            Arguments = arguments;
            NonBlocking = nonBlocking;
        }

        public override string ToString()
        {
            return $"Invocation {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(NonBlocking)}: {NonBlocking}, {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {string.Join(", ", Arguments.Select(a => a?.ToString()))} ] }}";
        }
    }
}
