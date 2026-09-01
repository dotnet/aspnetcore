// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.Core;
using Moq;
using Xunit;

namespace IIS.Tests;

public class HttpUpgradeStreamTests
{
    [Fact]
    public void FlushThrowsIfSynchronousIOIsDisallowed()
    {
        var bodyControl = new Mock<IHttpBodyControlFeature>(MockBehavior.Strict);
        bodyControl.SetupGet(feature => feature.AllowSynchronousIO).Returns(false);
        var responseStream = new HttpResponseStream(bodyControl.Object, context: null!);
        var stream = new HttpUpgradeStream(Stream.Null, responseStream);

        var exception = Assert.Throws<InvalidOperationException>(stream.Flush);

        Assert.Equal(CoreStrings.SynchronousWritesDisallowed, exception.Message);
    }
}
