// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal class TestCircuitIdFactory
{
    public static CircuitIdFactory CreateTestFactory()
    {
        return new CircuitIdFactory(new EphemeralDataProtectionProvider());
    }
}
