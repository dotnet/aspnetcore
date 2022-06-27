// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Count;
using Greet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests;

public class GrpcSwaggerServiceExtensionsTests
{
    [Fact]
    public void AddGrpcSwagger_GrpcServiceRegistered_ReturnSwaggerWithGrpcOperation()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddGrpcSwagger();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
        });
        services.AddRouting();
        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        app.UseRouting();
        app.UseEndpoints(c =>
        {
            c.MapGrpcService<GreeterService>();
        });

        var swaggerGenerator = serviceProvider.GetRequiredService<ISwaggerProvider>();
        var swagger = swaggerGenerator.GetSwagger("v1");

        // Assert
        Assert.NotNull(swagger);
        Assert.Single(swagger.Paths);

        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.True(path.Operations.ContainsKey(OperationType.Get));
    }

    [Fact]
    public void AddGrpcSwagger_GrpcServiceWithGroupName_FilteredByGroup()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddGrpcSwagger();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            c.SwaggerDoc("v2", new OpenApiInfo { Title = "My API", Version = "v2" });
        });
        services.AddRouting();
        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        app.UseRouting();
        app.UseEndpoints(c =>
        {
            c.MapGrpcService<GreeterService>();
            c.MapGrpcService<CounterService>();
        });

        var swaggerGenerator = serviceProvider.GetRequiredService<ISwaggerProvider>();

        // Assert 1
        var swagger = swaggerGenerator.GetSwagger("v1");
        Assert.Single(swagger.Paths);
        Assert.True(swagger.Paths["/v1/greeter/{name}"].Operations.ContainsKey(OperationType.Get));

        // Assert 2
        swagger = swaggerGenerator.GetSwagger("v2");
        Assert.Equal(2, swagger.Paths.Count);
        Assert.True(swagger.Paths["/v1/greeter/{name}"].Operations.ContainsKey(OperationType.Get));
        Assert.True(swagger.Paths["/v1/add/{value1}/{value2}"].Operations.ContainsKey(OperationType.Get));
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string ApplicationName { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; }
    }

    private class GreeterService : Greeter.GreeterBase
    {
    }

    [ApiExplorerSettings(GroupName = "v2")]
    private class CounterService : Counter.CounterBase
    {
    }
}
