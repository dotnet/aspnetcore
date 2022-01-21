// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using LocalizationTest.Abc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Localization.RootNamespace.Tests;

public class StringLocalizerOfTRootNamespaceTest
{
    [Fact]
    public void RootNamespace()
    {
        var locOptions = new LocalizationOptions();
        var options = new Mock<IOptions<LocalizationOptions>>();
        options.Setup(o => o.Value).Returns(locOptions);
        var factory = new ResourceManagerStringLocalizerFactory(options.Object, NullLoggerFactory.Instance);

        var valuesLoc = factory.Create(typeof(ValuesController));
        string value = valuesLoc["String1"]; // Note: Tests nullable analysis of implicit string conversion operator.
        Assert.Equal("ValFromResource", value);
    }
}
