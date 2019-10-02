// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class TestCircuitIdFactory
    {
        public static CircuitIdFactory CreateTestFactory()
        {
            return new CircuitIdFactory(new EphemeralDataProtectionProvider());
        }
    }
}
