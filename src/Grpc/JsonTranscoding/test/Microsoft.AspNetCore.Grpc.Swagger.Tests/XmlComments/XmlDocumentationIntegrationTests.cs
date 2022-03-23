// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Greet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.XmlComments;

public class XmlDocumentationIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XmlDocumentationIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ServiceDescription_ModelHasXmlDocs_UseXmlDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocServiceWithComments>();

        // Assert
        Assert.Equal("xmldoc.XmlDoc", swagger.Tags[0].Name);
        Assert.Equal("XmlDocServiceWithComments XML comment!", swagger.Tags[0].Description);
    }

    [Fact]
    public void ServiceDescription_ModelDoesntHaveXmlDocs_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocService>();

        // Assert
        Assert.Equal("xmldoc.XmlDoc", swagger.Tags[0].Name);
        Assert.Equal("XmlDoc!", swagger.Tags[0].Description);
    }

    [Fact]
    public void RouteParameter_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocServiceWithComments>();

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("Name field!", path.Operations[OperationType.Get].Parameters[0].Description);
    }

    [Fact]
    public void MethodDescription_ModelHasXmlDocs_UseXmlDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocServiceWithComments>();

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("BasicGet XML summary!", path.Operations[OperationType.Get].Summary);
        Assert.Equal("BasicGet XML remarks!", path.Operations[OperationType.Get].Description);
    }

    [Fact]
    public void MethodDescription_ModelDoesntHaveXmlDocs_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocService>();

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("BasicGet!", path.Operations[OperationType.Get].Summary);
        Assert.Null(path.Operations[OperationType.Get].Description);
    }

    [Fact]
    public void RequestDescription_Root_ModelHasXmlDocs_UseXmlDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocServiceWithComments>();

        // Assert
        var path = swagger.Paths["/v1/greeter"];
        Assert.Equal("Request XML param!", path.Operations[OperationType.Post].RequestBody.Description);
    }

    [Fact]
    public void RequestDescription_Root_ModelDoesntHaveXmlDocs_Empty()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocService>();

        // Assert
        var path = swagger.Paths["/v1/greeter"];
        Assert.Null(path.Operations[OperationType.Post].RequestBody.Description);
    }

    [Fact]
    public void RequestDescription_Nested_ProtoFieldDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocService>();

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("Detail field!", path.Operations[OperationType.Post].RequestBody.Description);
    }

    [Fact]
    public void Message_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = GetOpenApiDocument<XmlDocServiceWithComments>();

        // Assert
        var helloReplyMessage = swagger.Components.Schemas["StringReply"];
        Assert.Equal("StringReply!", helloReplyMessage.Description);
        Assert.Equal("Message field!", helloReplyMessage.Properties["message"].Description);
    }

    private OpenApiDocument GetOpenApiDocument<TService>() where TService : class
    {
        var services = new ServiceCollection();
        services.AddGrpcSwagger();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

            var filePath = Path.Combine(System.AppContext.BaseDirectory, "Microsoft.AspNetCore.Grpc.Swagger.Tests.xml");
            c.IncludeXmlComments(filePath);
            c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
        });
        services.AddRouting();
        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        app.UseRouting();
        app.UseEndpoints(c =>
        {
            c.MapGrpcService<TService>();
        });

        var swaggerGenerator = serviceProvider.GetRequiredService<ISwaggerProvider>();
        var swagger = swaggerGenerator.GetSwagger("v1");

        using var outputString = new StringWriter();
        swagger.SerializeAsV3(new OpenApiJsonWriter(outputString));
        _testOutputHelper.WriteLine(outputString.ToString());

        return swagger;
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
}
