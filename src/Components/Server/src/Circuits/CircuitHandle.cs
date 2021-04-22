// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    // Used to isolate a circuit from a CircuitHost.
    //
    // We can't refer to Hub.Items from a CircuitHost - but we want need to be
    // able to break the link between Hub.Items and a CircuitHost.
    internal class CircuitHandle
    {
        public CircuitHost CircuitHost { get; set; }
    }
}
