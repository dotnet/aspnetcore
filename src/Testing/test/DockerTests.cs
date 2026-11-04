// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class DockerTests
{
    [ConditionalFact]
    [DockerOnly]
    [Trait("Docker", "true")]
    public void DoesNotRunOnWindows()
    {
        Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }
}
