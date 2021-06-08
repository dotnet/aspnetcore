// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Lifetime;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal interface ICircuitHandleRegistry
    {
        CircuitHandle GetCircuitHandle(IDictionary<object, object?>  circuitHandles, object circuitKey);

        CircuitHost GetCircuit(IDictionary<object, object?>  circuitHandles, object circuitKey);

        void SetCircuit(IDictionary<object, object?>  circuitHandles, object circuitKey, CircuitHost circuitHost);
    }
}
