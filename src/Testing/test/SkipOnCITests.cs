// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests;

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
