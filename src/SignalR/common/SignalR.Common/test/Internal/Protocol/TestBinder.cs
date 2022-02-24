// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class TestBinder : IInvocationBinder
{
    private readonly Type[] _paramTypes;
    private readonly Type _returnType;

    public TestBinder(HubMessage expectedMessage)
    {
        switch (expectedMessage)
        {
            case StreamInvocationMessage i:
                _paramTypes = i.Arguments?.Select(a => a?.GetType() ?? typeof(object))?.ToArray();
                break;
            case InvocationMessage i:
                _paramTypes = i.Arguments?.Select(a => a?.GetType() ?? typeof(object))?.ToArray();
                break;
            case StreamItemMessage s:
                _returnType = s.Item?.GetType() ?? typeof(object);
                break;
            case CompletionMessage c:
                _returnType = c.Result?.GetType() ?? typeof(object);
                break;
        }
    }

    public TestBinder() : this(null, null) { }
    public TestBinder(Type[] paramTypes) : this(paramTypes, null) { }
    public TestBinder(Type returnType) : this(null, returnType) { }
    public TestBinder(Type[] paramTypes, Type returnType)
    {
        _paramTypes = paramTypes;
        _returnType = returnType;
    }

    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        if (_paramTypes != null)
        {
            return _paramTypes;
        }
        throw new InvalidOperationException("Unexpected binder call");
    }

    public Type GetReturnType(string invocationId)
    {
        if (_returnType != null)
        {
            return _returnType;
        }
        throw new InvalidOperationException("Unexpected binder call");
    }

    public Type GetStreamItemType(string streamId)
    {
        // In v1, stream items were only sent from server -> client
        // and so they had items typed based on what the hub method returned.
        // We just forward here for backwards compatibility.
        return GetReturnType(streamId);
    }
}
