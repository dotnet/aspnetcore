  
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class SkipOnHelixTests
    {
        [ConditionalFact]
        [SkipOnHelix("This should be skipped", Queues = "Windows.10.Amd64.Open")]
        public void SkipOnHelix()
        {
            throw new Exception("Flaky!");
        }
    }
}
