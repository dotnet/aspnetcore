// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class InvocationMessage : HubMessage
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

        public Exception ArgumentBindingException
        {
            get
            {
                return _argumentBindingException?.SourceException;
            }
        }

        public bool NonBlocking { get; }

        public InvocationMessage(string invocationId, bool nonBlocking, string target, ExceptionDispatchInfo argumentBindingException, params object[] arguments)
            : base(invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }

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
            NonBlocking = nonBlocking;
        }

        public override string ToString()
        {
            return $"Invocation {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(NonBlocking)}: {NonBlocking}, {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {string.Join(", ", Arguments.Select(a => a?.ToString()))} ] }}";
        }
    }
}
