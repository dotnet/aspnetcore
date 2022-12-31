// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Xml.XPath;
using Grpc.AspNetCore.Server;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Grpc.Swagger.Internal.XmlComments;

internal sealed class GrpcXmlCommentsOperationFilter : IOperationFilter
{
    private readonly XPathNavigator _xmlNavigator;

    public GrpcXmlCommentsOperationFilter(XPathDocument xmlDoc)
    {
        _xmlNavigator = xmlDoc.CreateNavigator();
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var grpcMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<GrpcMethodMetadata>().FirstOrDefault();
        if (grpcMetadata == null)
        {
            return;
        }

        var methodInfo = grpcMetadata.ServiceType.GetMethod(grpcMetadata.Method.Name);
        if (methodInfo == null)
        {
            return;
        }

        // If method is from a constructed generic type, look for comments from the generic type method
        var targetMethod = methodInfo.DeclaringType!.IsConstructedGenericType
            ? methodInfo.GetUnderlyingGenericTypeMethod()
            : methodInfo;

        if (targetMethod == null)
        {
            return;
        }

        // Base service never has response tags.
        ApplyServiceTags(operation, targetMethod.DeclaringType!);

        if (TryApplyMethodTags(operation, targetMethod))
        {
            return;
        }

        if (targetMethod.IsVirtual && targetMethod.GetBaseDefinition() is { } baseMethod)
        {
            if (TryApplyMethodTags(operation, baseMethod))
            {
                return;
            }
        }
    }

    private void ApplyServiceTags(OpenApiOperation operation, Type controllerType)
    {
        var typeMemberName = XmlCommentsNodeNameHelper.GetMemberNameForType(controllerType);
        var responseNodes = _xmlNavigator.Select($"/doc/members/member[@name='{typeMemberName}']/response");
        ApplyResponseTags(operation, responseNodes);
    }

    private bool TryApplyMethodTags(OpenApiOperation operation, MethodInfo methodInfo)
    {
        var methodMemberName = XmlCommentsNodeNameHelper.GetMemberNameForMethod(methodInfo);
        var methodNode = _xmlNavigator.SelectSingleNode($"/doc/members/member[@name='{methodMemberName}']");

        if (methodNode == null)
        {
            return false;
        }

        var summaryNode = methodNode.SelectSingleNode("summary");
        if (summaryNode != null)
        {
            operation.Summary = XmlCommentsTextHelper.Humanize(summaryNode.InnerXml);
        }

        var remarksNode = methodNode.SelectSingleNode("remarks");
        if (remarksNode != null)
        {
            operation.Description = XmlCommentsTextHelper.Humanize(remarksNode.InnerXml);
        }

        var responseNodes = methodNode.Select("response");
        ApplyResponseTags(operation, responseNodes);

        return true;
    }

    private static void ApplyResponseTags(OpenApiOperation operation, XPathNodeIterator responseNodes)
    {
        while (responseNodes.MoveNext())
        {
            var code = responseNodes.Current!.GetAttribute("code", "");
            if (!operation.Responses.TryGetValue(code, out var response))
            {
                operation.Responses[code] = response = new OpenApiResponse();
            }

            response.Description = XmlCommentsTextHelper.Humanize(responseNodes.Current.InnerXml);
        }
    }
}
