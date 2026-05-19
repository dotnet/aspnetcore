// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class NonInvokingSingleClientProxy : ISingleClientProxy
{
    private readonly IClientProxy _clientProxy;
    private readonly string _memberName;

    public NonInvokingSingleClientProxy(IClientProxy clientProxy, string memberName)
    {
        _clientProxy = clientProxy;
        _memberName = memberName;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) =>
        _clientProxy.SendCoreAsync(method, args, cancellationToken);

    public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException($"The default implementation of {_memberName} does not support client return results.");
}
