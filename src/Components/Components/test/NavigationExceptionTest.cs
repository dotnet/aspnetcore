// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class NavigationExceptionTest
{
    [Fact]
    public void Constructor_IncludesLocationInMessage()
    {
        var exception = new NavigationException("scheme://host/page");

        Assert.Equal("scheme://host/page", exception.Location);
        Assert.Equal("Could not navigate to 'scheme://host/page'.", exception.Message);
    }
}
