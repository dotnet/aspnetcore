// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Represents a failure to bind arguments for an invocation. This does not represent an actual
/// message that is sent on the wire, it is returned by <see cref="IHubProtocol.TryParseMessage"/>
/// to indicate that a binding failure occurred when parsing an invocation. The invocation ID is associated
/// so that the error can be sent back to the client, associated with the appropriate invocation ID.
/// </summary>
public class InvocationBindingFailureMessage : HubInvocationMessage
{
    /// <summary>
    /// Gets the exception thrown during binding.
    /// </summary>
    public ExceptionDispatchInfo BindingFailure { get; }

    /// <summary>
    /// Gets the target method name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationBindingFailureMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="bindingFailure">The exception thrown during binding.</param>
    public InvocationBindingFailureMessage(string? invocationId, string target, ExceptionDispatchInfo bindingFailure) : base(invocationId)
    {
        Target = target;
        BindingFailure = bindingFailure;
    }
}
