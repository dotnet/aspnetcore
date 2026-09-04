// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.Core;
using Xunit;

namespace IIS.Tests;

public class HttpResponseStreamTests
{
    [Fact]
    public void FlushThrowsIfSynchronousIOIsDisallowed()
    {
        var bodyControl = new TestHttpBodyControlFeature { AllowSynchronousIO = false };
        var stream = new HttpResponseStream(bodyControl, context: null!);

        var exception = Assert.Throws<InvalidOperationException>(stream.Flush);

        Assert.Equal(CoreStrings.SynchronousWritesDisallowed, exception.Message);
    }
}
