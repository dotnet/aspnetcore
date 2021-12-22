// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Xunit;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class UrlHelperExtensionsTests
{
    private static IUrlHelper GetMockUrlHelper()
    {
        var urlHelper = new Mock<IUrlHelper>();
        return urlHelper.Object;
    }

    [Theory]
    [InlineData("DoSomethingAsync", "DoSomething")]
    [InlineData("DoSomethingasync", "DoSomethingasync")]
    public void TestActionNameTrimming(string inputName, string expectedName)
    {
        var urlHelper = GetMockUrlHelper();
        string url = urlHelper.Action(inputName, null, null, null, null, null);

        Assert.Contains(expectedName, url);
        Assert.True(!url.Contains("Async"));
    }
}
