// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public abstract class HubMethodInvocationMessage : HubInvocationMessage
    {
        public string Target { get; }

        public object[] Arguments { get; }

        protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments)
            : base(invocationId)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            Target = target;
            Arguments = arguments;
        }
    }

    public class InvocationMessage : HubMethodInvocationMessage
    {
        public InvocationMessage(string target, object[] arguments)
            : this(null, target, arguments)
        {
        }

        public InvocationMessage(string invocationId, string target, object[] arguments)
            : base(invocationId, target, arguments)
        {
        }

        public override string ToString()
        {
            string args;
            try
            {
                args = string.Join(", ", Arguments?.Select(a => a?.ToString()));
            }
            catch (Exception ex)
            {
                args = $"Error: {ex.Message}";
            }
            return $"InvocationMessage {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {args} ] }}";
        }
    }

    public class StreamInvocationMessage : HubMethodInvocationMessage
    {
        public StreamInvocationMessage(string invocationId, string target, object[] arguments)
            : base(invocationId, target, arguments)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }
        }

        public override string ToString()
        {
            string args;
            try
            {
                args = string.Join(", ", Arguments?.Select(a => a?.ToString()));
            }
            catch (Exception ex)
            {
                args = $"Error: {ex.Message}";
            }
            return $"StreamInvocation {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {args} ] }}";
        }
    }
}
