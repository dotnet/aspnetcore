// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class SkipOnCITests
    {
        [ConditionalFact]
        [SkipOnCI]
        public void AlwaysSkipOnCI()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky!");
            }
        }
    }
}
