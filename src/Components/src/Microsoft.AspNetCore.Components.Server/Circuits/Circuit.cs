// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Represents an active connection between a Blazor server and a client.
    /// </summary>
    public class Circuit
    {
        /// <summary>
        /// Gets the current <see cref="Circuit"/>.
        /// </summary>
        public static Circuit Current => CircuitHost.Current?.Circuit;

        internal Circuit(CircuitHost circuitHost)
        {
            JSRuntime = circuitHost.JSRuntime;
            Services = circuitHost.Services;
        }

        /// <summary>
        /// Gets the <see cref="IJSRuntime"/> associated with this circuit.
        /// </summary>
        public IJSRuntime JSRuntime { get; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> associated with this circuit.
        /// </summary>
        public IServiceProvider Services { get; }
    }
}
