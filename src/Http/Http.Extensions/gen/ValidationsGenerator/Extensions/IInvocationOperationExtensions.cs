// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public static class IInvocationOperationExtensions
{
    public static string GetEndpointKey(this IInvocationOperation operation)
    {
        var routePattern = operation.GetRoutePattern();
        var httpMethods = operation.GetHttpMethods();
        return $@"new EndpointKey(""{routePattern}"", {httpMethods})";
    }

    private static string GetRoutePattern(this IInvocationOperation operation)
    {
        if (operation.Arguments.Length < 2)
        {
            throw new InvalidOperationException("The operation does not contain enough arguments to extract the route pattern.");
        }

        var routePatternArgument = operation.Arguments[1];
        if (routePatternArgument.Value.Syntax is LiteralExpressionSyntax literalExpression)
        {
            return literalExpression.Token.ValueText;
        }

        throw new InvalidOperationException("The route pattern argument is not a literal expression.");
    }

    private static string GetHttpMethods(this IInvocationOperation operation)
    {
        var syntax = (InvocationExpressionSyntax)operation.Syntax;
        var expression = (MemberAccessExpressionSyntax)syntax.Expression;
        var name = (IdentifierNameSyntax)expression.Name;
        var identifier = name.Identifier;
        var builder = new StringBuilder();
        builder.Append('[');
        if (identifier.ValueText == "MapMethods")
        {
            var methods = ExtractMapMethods(operation);
            builder.Append(string.Join(", ", methods.Select(method => @$"""{method}""")));
        }
        else
        {
            builder.Append('"');
            builder.Append(identifier.ValueText switch
            {
                "MapGet" => "GET",
                "MapPost" => "POST",
                "MapPut" => "PUT",
                "MapDelete" => "DELETE",
                "MapPatch" => "PATCH",
                _ => throw new InvalidOperationException("Unsupported HTTP method."),
            });
            builder.Append('"');
        }
        builder.Append(']');
        return builder.ToString();

        static List<string> ExtractMapMethods(IInvocationOperation operation)
        {
            var arguments = operation.Arguments;
            var methods = arguments[2].Value;
            if (methods.Syntax is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                var initializer = implicitArrayCreation.Initializer;
                if (initializer != null)
                {
                    return [.. initializer.Expressions
                        .OfType<LiteralExpressionSyntax>()
                        .Select(literal => literal.Token.ValueText)];
                }
            }

            return [];
        }
    }
}
