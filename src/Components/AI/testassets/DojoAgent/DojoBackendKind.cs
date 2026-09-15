// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoAgent;

/// <summary>
/// Identifies which transport a dojo scenario host uses to reach its model.
/// </summary>
public enum DojoBackendKind
{
    /// <summary>The AG-UI HTTP/SSE transport, backed by the AGUIDojoApi host.</summary>
    AGUI,

    /// <summary>The in-process <see cref="Microsoft.Extensions.AI.IChatClient"/> transport.</summary>
    Direct,
}
