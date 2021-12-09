// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleWithWebApplicationBuilderExceptionTests : IClassFixture<MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program>>
{
    private readonly MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program> _fixture;

    public SimpleWithWebApplicationBuilderExceptionTests(MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ExceptionThrownFromApplicationCanBeObserved()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _fixture.CreateClient());
        Assert.Equal("This application failed to start", ex.Message);
    }
}
