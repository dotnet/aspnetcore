// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Client.Internal;

/// <summary>
/// Thrown by the client's <see cref="IInvocationBinder.GetParameterTypes(string)"/> when the server invokes a
/// method the client has no handler for. It lets the message loop distinguish this benign, intentional case
/// (e.g. the server broadcasting to clients that never registered the handler) from a genuine argument-binding
/// failure, so the former is not logged as an error.
/// </summary>
internal sealed class HubMethodDoesNotExistException : HubException
{
    public HubMethodDoesNotExistException(string methodName)
        : base($"Method '{methodName}' does not exist.")
    {
    }
}
