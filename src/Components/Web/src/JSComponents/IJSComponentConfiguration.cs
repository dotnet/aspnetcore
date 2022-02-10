// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Configures options for allowing JavaScript to add root components dynamically.
/// </summary>
public interface IJSComponentConfiguration
{
    /// <summary>
    /// Gets the store of configuration options that allow JavaScript to add root components dynamically.
    /// </summary>
    JSComponentConfigurationStore JSComponents { get; }
}
