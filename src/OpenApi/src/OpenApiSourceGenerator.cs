// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Microsoft.AspNetCore.OpenApi;

[Generator]
public class OpenApiSourceGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var operations = context.SyntaxProvider.CreateSyntaxProvider<(string RoutePattern, OperationType OperationType, OpenApiOperation Operation)>(
            predicate: static (node, _) => node.TryGetMapMethodName(out var method) && InvocationOperationExtensions.KnownMethods.Contains(method),
            transform: static (context, token) =>
            {
                context.Node.TryGetMapMethodName(out var method);
                var operationType = method switch
                {
                    "MapGet" => OperationType.Get,
                    "MapPost" => OperationType.Post,
                    "MapPut" => OperationType.Put,
                    "MapDelete" => OperationType.Delete,
                    "MapPatch" => OperationType.Patch,
                    "MapHead" => OperationType.Head,
                    "MapOptions" => OperationType.Options,
                    _ => OperationType.Get
                };
                TryGetRouteHandlerPattern((InvocationExpressionSyntax)context.Node, method, out var routePattern);
                if (!context.Node.HasLeadingTrivia)
                {
                    return (routePattern, operationType, new OpenApiOperation())!;
                }

                var trivia = context.Node.GetLeadingTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
                var node = trivia.GetStructure();
                var openApiResponses = new OpenApiResponses();
                var openApiParameters = new List<OpenApiParameter>();
                var summary = string.Empty;
                var description = string.Empty;
                if (node is DocumentationCommentTriviaSyntax docs)
                {
                    var nodes = docs.Content;
                    // Types of doc comments to support:
                    // - <response>
                    // - <tags>
                    // - <remarks>
                    // - <summary>
                    // - <param>
                    //  - <summary>
                    //  - <example>
                    //  - <contentType>
                    foreach (var target in nodes)
                    {
                        if (target is XmlElementSyntax { StartTag: { Name: { LocalName: { Text: "summary" } } } } summaryElement)
                        {
                            summary = summaryElement.Content[0].GetText().ToString();
                        }

                        if (target is XmlElementSyntax { StartTag: { Name: { LocalName: { Text: "remarks" } } } } descriptionElement)
                        {
                            description = descriptionElement.Content[0].GetText().ToString();
                        }
                        if (target is XmlElementSyntax { StartTag: { Name: { LocalName: { Text: "response" } } } } responseElement)
                        {
                            var statusCode = "200";
                            foreach (var attribute in responseElement.StartTag.Attributes)
                            {
                                if (attribute is XmlTextAttributeSyntax { Name: { LocalName: { Text: "code" } } } statusAttribute)
                                {
                                    statusCode = statusAttribute.TextTokens[0].Text;
                                }
                            }
                            openApiResponses.Add(statusCode, new OpenApiResponse
                            {
                                Description = responseElement.Content[0].GetText().ToString()
                            });
                        }

                        if (target is XmlElementSyntax { StartTag: { Name: { LocalName: { Text: "param" } } } } paramElement)
                        {
                            var name = string.Empty;
                            foreach (var attribute in paramElement.StartTag.Attributes)
                            {
                                if (attribute is XmlNameAttributeSyntax nameAttribute)
                                {
                                    name = nameAttribute.Identifier.Identifier.Text;
                                }
                            }
                            openApiParameters.Add(new OpenApiParameter
                            {
                                Description = paramElement.Content[0].GetText().ToString(),
                                Name = name,
                            });
                        }
                    }
                }
                return (routePattern, operationType, new OpenApiOperation
                {
                    Summary = summary,
                    Description = description,
                    Responses = openApiResponses,
                    Parameters = openApiParameters
                })!;
            });

        context.RegisterSourceOutput(operations.Collect(), (productionContext, ops) =>
        {
            var textWriter = new StringWriter();
            var jsonWriter = new OpenApiJsonWriter(textWriter);
            if (ops.IsDefaultOrEmpty)
            {
                return;
            }
            var openApiDocument = new OpenApiDocument
            {
                Paths = new OpenApiPaths()
            };
            var paths = ops.GroupBy((element) =>
            {
                (string routePattern, OperationType operationType, OpenApiOperation operation) = element;
                return routePattern;
            });
            foreach (var group in paths)
            {
                var pathItem = new OpenApiPathItem();
                foreach (var (routePattern, operationType, operation) in group)
                {
                    pathItem.Operations[operationType] = operation;
                }
                openApiDocument.Paths.Add(group.Key, pathItem);
            }
            openApiDocument.Components = new OpenApiComponents();
            openApiDocument.SerializeAsV3(jsonWriter);
 #pragma warning disable RS1035
            File.WriteAllText("/Users/captainsafia/gh/aspnetcore/src/OpenApi/openapi.json", textWriter.ToString());
 #pragma warning restore RS1035
        });
    }

    public static bool TryGetRouteHandlerPattern(InvocationExpressionSyntax invocation, string? methodName, [NotNullWhen(true)] out string? token)
    {
        token = default;
        if (methodName == "MapFallback" && invocation.ArgumentList.Arguments.Count == 2)
        {
            token = "{*path:nonfile}";
            return true;
        }
        var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (argument is not null)
        {
            token = argument.Expression.ToString();
            return true;
        }
        return false;
    }
}
