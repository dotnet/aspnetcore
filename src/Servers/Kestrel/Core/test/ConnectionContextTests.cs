// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ConnectionContextTests
{
    [Fact]
    public void ParameterlessAbortCreateConnectionAbortedException()
    {
        var connectionContext = new TestConnectionContext();

        connectionContext.Abort();

        var ex = Assert.Single(connectionContext.AbortReasons);

        Assert.NotNull(ex);
        Assert.Equal("The connection was aborted by the application via ConnectionContext.Abort().", ex.Message);
    }
}
