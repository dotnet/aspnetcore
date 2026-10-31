// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Options for root components within the circuit.
/// </summary>
public class CircuitRootComponentOptions : IJSComponentConfiguration
{
    /// <inheritdoc />
    public JSComponentConfigurationStore JSComponents { get; } = new();

    /// <summary>
    /// Gets or sets the maximum number of root components added from JavaScript.
    /// </summary>
    /// <value>Defaults to <c>100</c>.</value>
    public int MaxJSRootComponents { get; set; } = 100;
}
