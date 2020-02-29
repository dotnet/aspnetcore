// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class QuarantinedTestAttributeTest
    {
        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [QuarantinedTest]
        public void AlwaysFlakyInCI()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky!");
            }
        }
    }
}
