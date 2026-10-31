// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class CompositeTestBinder : IInvocationBinder
{
    private readonly HubMessage[] _hubMessages;
    private int index = 0;

    public CompositeTestBinder(HubMessage[] hubMessages)
    {
        _hubMessages = hubMessages.Where(IsBindableMessage).ToArray();
    }

    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        index++;
        return new TestBinder(_hubMessages[index - 1]).GetParameterTypes(methodName);
    }

    public Type GetReturnType(string invocationId)
    {
        index++;
        return new TestBinder(_hubMessages[index - 1]).GetReturnType(invocationId);
    }

    private bool IsBindableMessage(HubMessage arg)
    {
        return arg is CompletionMessage ||
            arg is InvocationMessage ||
            arg is StreamItemMessage ||
            arg is StreamInvocationMessage;
    }

    public Type GetStreamItemType(string streamId)
    {
        throw new NotImplementedException();
    }
}
