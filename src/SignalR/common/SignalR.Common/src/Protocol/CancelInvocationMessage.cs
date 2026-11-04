// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// The <see cref="CancelInvocationMessage"/> represents a cancellation of a streaming method.
/// </summary>
public class CancelInvocationMessage : HubInvocationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The ID of the hub method invocation being canceled.</param>
    public CancelInvocationMessage(string invocationId) : base(invocationId)
    {
    }
}
