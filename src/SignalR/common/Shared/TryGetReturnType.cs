// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.SignalR.Protocol;

internal static class ProtocolHelper
{
    internal static Type? TryGetReturnType(IInvocationBinder binder, string invocationId)
    {
        try
        {
            return binder.GetReturnType(invocationId);
        }
        // GetReturnType throws if invocationId not found, this can be caused by the server canceling a client-result but the client still sending a result
        // For now let's ignore the failure and skip parsing the result, server will log that the result wasn't expected anymore and ignore the message
        // In the future we may want a CompletionBindingFailureMessage that we can flow to the dispatcher for handling
        catch (Exception)
        {
            return null;
        }
    }
}
