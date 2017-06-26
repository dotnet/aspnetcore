using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class CompositeTestBinder : IInvocationBinder
    {
        private readonly HubMessage[] _hubMessages;
        private int index = 0;

        public CompositeTestBinder(HubMessage[] hubMessages)
        {
            _hubMessages = hubMessages;
        }

        public Type[] GetParameterTypes(string methodName)
        {
            index++;
            return new TestBinder(_hubMessages[index - 1]).GetParameterTypes(methodName);
        }

        public Type GetReturnType(string invocationId)
        {
            index++;
            return new TestBinder(_hubMessages[index - 1]).GetReturnType(invocationId);
        }
    }
}
