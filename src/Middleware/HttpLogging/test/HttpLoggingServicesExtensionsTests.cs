// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.HttpLogging;

public class HttpLoggingServicesExtensionsTests
{
    [Fact]
    public void AddHttpLogging_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddHttpLogging(null));
    }

    [Fact]
    public void AddW3CLogging_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddW3CLogging(null));
    }
}
