// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Represents a failure to bind arguments for a StreamDataMessage. This does not represent an actual
/// message that is sent on the wire, it is returned by <see cref="IHubProtocol.TryParseMessage"/>
/// to indicate that a binding failure occurred when parsing a StreamDataMessage. The stream ID is associated
/// so that the error can be sent to the relevant hub method.
/// </summary>
public class StreamBindingFailureMessage : HubMessage
{
    /// <summary>
    /// Gets the id of the relevant stream
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the exception thrown during binding.
    /// </summary>
    public ExceptionDispatchInfo BindingFailure { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationBindingFailureMessage"/> class.
    /// </summary>
    /// <param name="id">The stream ID.</param>
    /// <param name="bindingFailure">The exception thrown during binding.</param>
    public StreamBindingFailureMessage(string id, ExceptionDispatchInfo bindingFailure)
    {
        Id = id;
        BindingFailure = bindingFailure;
    }
}
