// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;
using Microsoft.AspNetCore.Grpc.Swagger.Internal.XmlComments;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the gRPC JSON transcoding services.
/// </summary>
public static class GrpcSwaggerGenOptionsExtensions
{
    /// <summary>
    /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="xmlDocFactory">A factory method that returns XML Comments as an XPathDocument</param>
    public static void IncludeGrpcXmlComments(
        this SwaggerGenOptions swaggerGenOptions,
        Func<XPathDocument> xmlDocFactory)
    {
        swaggerGenOptions.IncludeGrpcXmlComments(xmlDocFactory, includeControllerXmlComments: false);
    }

    /// <summary>
    /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="xmlDocFactory">A factory method that returns XML Comments as an XPathDocument</param>
    /// <param name="includeControllerXmlComments">
    /// Flag to indicate if controller XML comments (i.e. summary) should be used to assign Tag descriptions.
    /// Don't set this flag if you're customizing the default tag for operations via TagActionsBy.
    /// </param>
    public static void IncludeGrpcXmlComments(
        this SwaggerGenOptions swaggerGenOptions,
        Func<XPathDocument> xmlDocFactory,
        bool includeControllerXmlComments)
    {
        var xmlDoc = xmlDocFactory();
        swaggerGenOptions.OperationFilter<GrpcXmlCommentsOperationFilter>(xmlDoc);

        if (includeControllerXmlComments)
        {
            swaggerGenOptions.DocumentFilter<GrpcXmlCommentsDocumentFilter>(xmlDoc);
        }
    }

    /// <summary>
    /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="filePath">An absolute path to the file that contains XML Comments</param>
    public static void IncludeGrpcXmlComments(
        this SwaggerGenOptions swaggerGenOptions,
        string filePath)
    {
        swaggerGenOptions.IncludeGrpcXmlComments(() => new XPathDocument(filePath));
    }

    /// <summary>
    /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="filePath">An absolute path to the file that contains XML Comments</param>
    /// <param name="includeControllerXmlComments">
    /// Flag to indicate if controller XML comments (i.e. summary) should be used to assign Tag descriptions.
    /// Don't set this flag if you're customizing the default tag for operations via TagActionsBy.
    /// </param>
    public static void IncludeGrpcXmlComments(
        this SwaggerGenOptions swaggerGenOptions,
        string filePath,
        bool includeControllerXmlComments)
    {
        swaggerGenOptions.IncludeGrpcXmlComments(() => new XPathDocument(filePath), includeControllerXmlComments);
    }
}
