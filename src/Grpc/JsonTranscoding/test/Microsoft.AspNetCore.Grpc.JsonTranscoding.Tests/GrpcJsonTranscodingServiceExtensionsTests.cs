// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class GrpcJsonTranscodingServiceExtensionsTests
{
    [Fact]
    public void AddGrpcJsonTranscoding_DefaultOptions_PopulatedProperties()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGrpc().AddJsonTranscoding();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options1 = serviceProvider.GetRequiredService<IOptions<GrpcJsonTranscodingOptions>>().Value;

        Assert.NotNull(options1.JsonSettings);

        var options2 = serviceProvider.GetRequiredService<IOptions<GrpcJsonTranscodingOptions>>().Value;

        Assert.Equal(options1, options2);
    }

    [Fact]
    public void AddGrpcJsonTranscoding_OverrideOptions_OptionsApplied()
    {
        // Arrange
        var settings = new GrpcJsonSettings();

        var services = new ServiceCollection();

        // Act
        services.AddGrpc().AddJsonTranscoding(o =>
        {
            o.JsonSettings = settings;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<GrpcJsonTranscodingOptions>>().Value;

        Assert.Equal(settings, options.JsonSettings);
    }
}
