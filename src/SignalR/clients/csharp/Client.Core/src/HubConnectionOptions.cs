// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Configures timeouts for the <see cref="HubConnection" />.
/// </summary>
internal sealed class HubConnectionOptions
{
    /// <summary>
    /// Configures ServerTimeout for the <see cref="HubConnection" />.
    /// </summary>
    public TimeSpan? ServerTimeout { get; set; }

    /// <summary>
    /// Configures KeepAliveInterval for the <see cref="HubConnection" />.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }
}
