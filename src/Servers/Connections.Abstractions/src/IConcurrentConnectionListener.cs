// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Indicates that a <see cref="IConnectionListener"/> or <see cref="IMultiplexedConnectionListener"/> supports concurrent accept operations.
/// </summary>
public interface IConcurrentConnectionListener
{
    /// <summary>
    /// The maximum number of concurrent accept operations supported by this listener.
    /// </summary>
    int MaxAccepts { get; }

    /// <summary>
    /// Accepts incoming connections as a sequence of opaque tokens (to be used via <see cref="Accept"/>.
    /// </summary>
    /// <remarks>The token returned allows deferral of some setup operations, so that the accept loop can resume directly</remarks>
    IAsyncEnumerable<object> AcceptManyAsync();

    /// <summary>
    /// Finishes accepting a connection with a token provided by <see cref="AcceptManyAsync"/>
    /// </summary>
    ConnectionContext Accept(object token);
}
