// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides what a projection needs when it is created, shared by all projections of the scope.
/// </summary>
internal sealed class FeatureContext
{
    public FeatureContext(IJSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the JS runtime of the scope.
    /// </summary>
    public IJSRuntime JSRuntime { get; }
}
