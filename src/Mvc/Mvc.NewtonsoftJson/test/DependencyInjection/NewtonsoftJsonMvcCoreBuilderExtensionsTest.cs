// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public class NewtonsoftJsonMvcCoreBuilderExtensionsTest
{
    [Fact]
    public void AddNewtonsoftJson_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMvcCore()
            .AddNewtonsoftJson((options) =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

        // Assert
        Assert.Single(services, d => d.ServiceType == typeof(IConfigureOptions<MvcNewtonsoftJsonOptions>));
    }

    [Fact]
    public void AddServicesCore_ReplacesDefaultJsonHelper()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IJsonHelper, SystemTextJsonHelper>();

        // Act
        NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(services);

        // Assert
        var jsonHelper = Assert.Single(services, d => d.ServiceType == typeof(IJsonHelper));
        Assert.Same(typeof(NewtonsoftJsonHelper), jsonHelper.ImplementationType);
    }

    [Fact]
    public void AddServicesCore_ReplacesDefaultTempDataSerializer()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<TempDataSerializer, DefaultTempDataSerializer>();

        // Act
        NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(services);

        // Assert
        var tempDataSerializer = Assert.Single(services, d => d.ServiceType == typeof(TempDataSerializer));
        Assert.Same(typeof(BsonTempDataSerializer), tempDataSerializer.ImplementationType);
    }

    [Fact]
    public void AddServicesCore_ReplacesDefaultJsonResultExecutor()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IActionResultExecutor<JsonResult>, SystemTextJsonResultExecutor>();

        // Act
        NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(services);

        // Assert
        var jsonResultExecutor = Assert.Single(services, d => d.ServiceType == typeof(IActionResultExecutor<JsonResult>));
        Assert.Same(typeof(NewtonsoftJsonResultExecutor), jsonResultExecutor.ImplementationType);
    }
}
