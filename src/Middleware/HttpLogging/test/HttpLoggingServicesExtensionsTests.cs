// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public void UseHttpLogging_RequireServices()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => appBuilder.UseHttpLogging());
        Assert.Equal("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHttpLogging' in the application startup code.", ex.Message);
    }

    [Fact]
    public void UseW3CLogging_RequireServices()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => appBuilder.UseW3CLogging());
        Assert.Equal("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddW3CLogging' in the application startup code.", ex.Message);
    }

    [Fact]
    public async Task UseHttpLogging_DoNotThrowWithoutOptions()
    {
        var services = new ServiceCollection();
        services.AddHttpLogging();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        appBuilder.UseHttpLogging();
        var app = appBuilder.Build();
        var context = new DefaultHttpContext();
        var exception = await Record.ExceptionAsync(() => app.Invoke(context));

        // Assert
        Assert.Null(exception);
    }
}
