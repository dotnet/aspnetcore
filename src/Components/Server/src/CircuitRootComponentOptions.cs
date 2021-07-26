// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server
{
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
        public int MaxJSRootComponents { get; set; } = 100;
    }
}
