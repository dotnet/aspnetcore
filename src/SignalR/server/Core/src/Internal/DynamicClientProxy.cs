// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class DynamicClientProxy : DynamicObject
{
    private readonly IClientProxy _clientProxy;

    public DynamicClientProxy(IClientProxy clientProxy)
    {
        _clientProxy = clientProxy;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        result = _clientProxy.SendCoreAsync(binder.Name, args!);
        return true;
    }
}
