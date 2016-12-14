using System;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IInvocationBinder
    {
        Type GetReturnType(string invocationId);
        Type[] GetParameterTypes(string methodName);
    }
}
