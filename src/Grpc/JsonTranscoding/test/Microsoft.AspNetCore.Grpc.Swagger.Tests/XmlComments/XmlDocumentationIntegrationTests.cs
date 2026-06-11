// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Greet;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi;
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
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocServiceWithComments>(_testOutputHelper);

        // Assert
        var tag = Assert.Single(swagger.Tags);
        Assert.Equal("XmlDoc", tag.Name);
        // Swashbuckle 10.2.1 no longer applies service-level XML comments to the generated tag description.
        Assert.Null(tag.Description);
    }

    [Fact]
    public void ServiceDescription_ModelDoesntHaveXmlDocs_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocService>(_testOutputHelper);

        // Assert
        var tag = Assert.Single(swagger.Tags);
        Assert.Equal("XmlDoc", tag.Name);
        // Swashbuckle 10.2.1 no longer applies service-level proto comments to the generated tag description.
        Assert.Null(tag.Description);
    }

    [Fact]
    public void RouteParameter_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocServiceWithComments>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("Name field!", path.Operations[HttpMethod.Get].Parameters[0].Description);
    }

    [Fact]
    public void MethodDescription_ModelHasXmlDocs_UseXmlDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocServiceWithComments>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("BasicGet XML summary!", path.Operations[HttpMethod.Get].Summary);
        Assert.Equal("BasicGet XML remarks!", path.Operations[HttpMethod.Get].Description);
    }

    [Fact]
    public void MethodDescription_ModelDoesntHaveXmlDocs_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("BasicGet!", path.Operations[HttpMethod.Get].Summary);
        Assert.Null(path.Operations[HttpMethod.Get].Description);
    }

    [Fact]
    public void RequestDescription_Root_ModelHasXmlDocs_UseXmlDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocServiceWithComments>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter"];
        Assert.Equal("Request XML param!", path.Operations[HttpMethod.Post].RequestBody.Description);
    }

    [Fact]
    public void RequestDescription_Root_ModelDoesntHaveXmlDocs_Empty()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter"];
        Assert.Null(path.Operations[HttpMethod.Post].RequestBody.Description);
    }

    [Fact]
    public void RequestDescription_Nested_ProtoFieldDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter/{name}"];
        Assert.Equal("Detail field!", path.Operations[HttpMethod.Post].RequestBody.Description);
    }

    [Fact]
    public void Parameters_QueryParameters_ProtoFieldDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/greeter/query/{name}"];
        Assert.Collection(path.Operations[HttpMethod.Get].Parameters,
            p =>
            {
                Assert.Equal(ParameterLocation.Path, p.In);
                Assert.Equal("name", p.Name);
                Assert.Equal("Name field!", p.Description);
            },
            p =>
            {
                Assert.Equal(ParameterLocation.Query, p.In);
                Assert.Equal("detail.age", p.Name);
                Assert.Equal("Age field!", p.Description);
            });
    }

    [Fact]
    public void Message_UseProtoDocs()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<XmlDocServiceWithComments>(_testOutputHelper);

        // Assert
        var helloReplyMessage = swagger.Components.Schemas["StringReply"];
        Assert.Equal("StringReply!", helloReplyMessage.Description);
        Assert.Equal("Message field!", helloReplyMessage.Properties["message"].Description);
    }

    private class GreeterService : Greeter.GreeterBase
    {
    }
}
