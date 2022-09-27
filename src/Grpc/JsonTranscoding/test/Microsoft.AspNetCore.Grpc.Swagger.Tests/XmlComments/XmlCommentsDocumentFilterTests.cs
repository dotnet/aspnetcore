// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Microsoft.AspNetCore.Grpc.Swagger.Internal.XmlComments;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.XmlComments;

public class XmlCommentsDocumentFilterTests
{
    private class TestMethod : IMethod
    {
        public MethodType Type { get; }
        public string ServiceName { get; } = "TestServiceName";
        public string Name { get; } = "TestName";
        public string FullName => ServiceName + "." + Name;
    }

    [Theory]
    [InlineData(typeof(XmlDocService), "XmlDoc!")]
    [InlineData(typeof(XmlDocServiceWithComments), "XmlDocServiceWithComments XML comment!")]
    public void Apply_SetsTagDescription_FromControllerSummaryTags(Type serviceType, string expectedDescription)
    {
        var document = new OpenApiDocument();
        var filterContext = new DocumentFilterContext(
            new[]
            {
                CreateApiDescription(serviceType),
                CreateApiDescription(serviceType)
            },
            null,
            null);

        Subject().Apply(document, filterContext);

        Assert.Equal(1, document.Tags.Count);
        Assert.Equal(expectedDescription, document.Tags[0].Description);

        static ApiDescription CreateApiDescription(Type serviceType)
        {
            return new ApiDescription
            {
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues =
                    {
                        ["controller"] = "greet.Greeter"
                    },
                    EndpointMetadata = new List<object>
                    {
                        new GrpcMethodMetadata(serviceType, new TestMethod())
                    }
                }
            };
        }
    }

    private GrpcXmlCommentsDocumentFilter Subject()
    {
        using (var xmlComments = File.OpenText($"{typeof(GreeterService).Assembly.GetName().Name}.xml"))
        {
            return new GrpcXmlCommentsDocumentFilter(new XPathDocument(xmlComments));
        }
    }
}
