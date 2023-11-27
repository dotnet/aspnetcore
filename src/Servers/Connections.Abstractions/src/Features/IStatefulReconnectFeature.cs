// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Abstractions;

/// <summary>
/// Provides access to connection reconnect operations.
/// </summary>
/// <remarks>This feature is experimental.</remarks>
#if NET8_0_OR_GREATER
[RequiresPreviewFeatures("IStatefulReconnectFeature is a preview interface")]
#endif
public interface IStatefulReconnectFeature
{
    /// <summary>
    /// Called when a connection reconnects. The new <see cref="PipeWriter"/> that application code should write to is passed in.
    /// </summary>
    public void OnReconnected(Func<PipeWriter, Task> notifyOnReconnect);

    /// <summary>
    /// Allows disabling the reconnect feature so a reconnecting connection will not be allowed anymore.
    /// </summary>
    void DisableReconnect();
}
