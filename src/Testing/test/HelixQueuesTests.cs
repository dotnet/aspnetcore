// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests;

public class HelixQueuesTests
{
    [ConditionalFact]
    [HelixQueues(Includes = "*", Excludes = "All.Windows")] // Run everywhere except for Windows.
    public void Test_Should_Be_Skipped_On_Windows()
    {
        var queue = HelixHelper.GetTargetHelixQueue();

        if (queue.StartsWith("windows", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This test should not run on Windows!");
        }
    }

    [ConditionalFact]
    [HelixQueues(Includes = "*", Excludes = "All.OSX")]
    public void Test_Should_Be_Skipped_On_OSX()
    {
        var queue = HelixHelper.GetTargetHelixQueue();

        if (queue.StartsWith("osx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This test should not run on OSX!");
        }
    }

    [ConditionalFact]
    [HelixQueues(Includes = "*", Excludes = "All.Ubuntu")]
    public void Test_Should_Be_Skipped_On_Ubuntu()
    {
        var queue = HelixHelper.GetTargetHelixQueue();

        if (queue.StartsWith("ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This test should not run on Ubuntu!");
        }
    }
}
