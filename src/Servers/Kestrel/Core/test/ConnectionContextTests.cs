// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ConnectionContextTests
{
    [Fact]
    public void ParameterlessAbortCreateConnectionAbortedException()
    {
        var mockConnectionContext = new Mock<ConnectionContext> { CallBase = true };
        ConnectionAbortedException ex = null;

        mockConnectionContext.Setup(c => c.Abort(It.IsAny<ConnectionAbortedException>()))
                             .Callback<ConnectionAbortedException>(abortReason => ex = abortReason);

        mockConnectionContext.Object.Abort();

        Assert.NotNull(ex);
        Assert.Equal("The connection was aborted by the application via ConnectionContext.Abort().", ex.Message);
    }
}
