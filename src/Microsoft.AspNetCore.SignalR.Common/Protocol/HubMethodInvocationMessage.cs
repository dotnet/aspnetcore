// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public abstract class HubMethodInvocationMessage : HubInvocationMessage
    {
        private readonly ExceptionDispatchInfo _argumentBindingException;
        private readonly object[] _arguments;

        public string Target { get; }

        public object[] Arguments
        {
            get
            {
                if (_argumentBindingException != null)
                {
                    _argumentBindingException.Throw();
                }

                return _arguments;
            }
        }

        public Exception ArgumentBindingException => _argumentBindingException?.SourceException;

        protected HubMethodInvocationMessage(string invocationId, string target, ExceptionDispatchInfo argumentBindingException, object[] arguments)
            : base(invocationId)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            if ((arguments == null && argumentBindingException == null) || (arguments?.Length > 0 && argumentBindingException != null))
            {
                throw new ArgumentException($"'{nameof(argumentBindingException)}' and '{nameof(arguments)}' are mutually exclusive");
            }

            Target = target;
            _arguments = arguments;
            _argumentBindingException = argumentBindingException;
        }
    }

    public class InvocationMessage : HubMethodInvocationMessage
    {
        public InvocationMessage(string target, ExceptionDispatchInfo argumentBindingException, object[] arguments)
            : this(null, target, argumentBindingException, arguments)
        {
        }

        public InvocationMessage(string invocationId, string target, ExceptionDispatchInfo argumentBindingException, object[] arguments)
            : base(invocationId, target, argumentBindingException, arguments)
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
        public StreamInvocationMessage(string invocationId, string target, ExceptionDispatchInfo argumentBindingException, object[] arguments)
            : base(invocationId, target, argumentBindingException, arguments)
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
