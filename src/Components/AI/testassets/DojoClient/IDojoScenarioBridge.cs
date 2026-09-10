// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient;

/// <summary>
/// Adapts a dojo scenario page to its configured backend without exposing transport details
/// to the page.
/// </summary>
internal interface IDojoScenarioBridge
{
    /// <summary>
    /// Creates the chat options that carry the current UI-owned state and thread identity to
    /// the configured backend.
    /// </summary>
    /// <param name="getContext">Produces the request context lazily, at request time.</param>
    /// <returns>The scenario's per-request chat options.</returns>
    ChatOptions CreateStateOptions(Func<DojoRequestContext> getContext);
}
