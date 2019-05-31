// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class TestCircuitIdFactory
    {
        public static CircuitIdFactory CreateTestFactory()
        {
            return new CircuitIdFactory(Options.Create(new CircuitOptions
            {
                CircuitIdProtector = new EphemeralDataProtectionProvider().CreateProtector("Test")
            }));
        }
    }
}
