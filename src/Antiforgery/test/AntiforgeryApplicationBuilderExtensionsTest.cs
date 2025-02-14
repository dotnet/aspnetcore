// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class AntiforgeryApplicationBuilderExtensionsTest
{
    [Fact]
    public async Task UseAntiforgery_HasRequiredServices_RegistersMiddleware()
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var services = CreateServices(antiforgeryService.Object);
        var builder = new ApplicationBuilder(services);
        var httpContext = AntiforgeryMiddlewareTest.GetHttpContext();

        Assert.False(builder.Properties.ContainsKey("__AntiforgeryMiddlewareSet"));

        builder.UseAntiforgery();

        Assert.True(builder.Properties.ContainsKey("__AntiforgeryMiddlewareSet"));

        var app = builder.Build();

        await app(httpContext);

        antiforgeryService.Verify(antiforgeryService => antiforgeryService.ValidateRequestAsync(httpContext), Times.AtMostOnce());
    }

    [Fact]
    public void UseAntiforgery_MissingRequiredServices_ThrowsException()
    {
        var services = CreateServices();
        var builder = new ApplicationBuilder(services);

        Assert.False(builder.Properties.ContainsKey("__AntiforgeryMiddlewareSet"));
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseAntiforgery());
        Assert.False(builder.Properties.ContainsKey("__AntiforgeryMiddlewareSet"));

        Assert.Equal(
            "Unable to find the required services. Please add all the required services by calling " +
            "'IServiceCollection.AddAntiforgery' in the application startup code.",
            exception.Message);
    }

    private IServiceProvider CreateServices(IAntiforgery? antiforgeryService = null)
    {
        var services = new ServiceCollection();
        if (antiforgeryService is not null)
        {
            services.AddSingleton(antiforgeryService);
        }
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }
}
