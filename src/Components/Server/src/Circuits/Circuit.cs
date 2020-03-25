// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Represents a link between a ASP.NET Core Component on the server and a client.
    /// </summary>
    public sealed class Circuit
    {
        private readonly CircuitHost _circuitHost;

        internal Circuit(CircuitHost circuitHost)
        {
            _circuitHost = circuitHost;
        }

        /// <summary>
        /// Gets the identifier for the <see cref="Circuit"/>.
        /// </summary>
        public string Id => _circuitHost.CircuitId.Id;
    }
}
