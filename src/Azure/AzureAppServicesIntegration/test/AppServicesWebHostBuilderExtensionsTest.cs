// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Hosting.Azure.AppServices.Tests;

public class AppServicesWebHostBuilderExtensionsTest
{
    [Fact]
    public void UseAzureAppServices_RegisterLogger()
    {
        var mock = new Mock<IWebHostBuilder>();

        mock.Object.UseAzureAppServices();

        mock.Verify(builder => builder.ConfigureServices(It.IsNotNull<Action<IServiceCollection>>()), Times.Once);
    }
}
