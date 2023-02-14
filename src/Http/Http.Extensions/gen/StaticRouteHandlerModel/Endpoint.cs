// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal class Endpoint
{
    private string? _argumentListCache;

    public Endpoint(IInvocationOperation operation, WellKnownTypes wellKnownTypes)
    {
        Operation = operation;
        Location = GetLocation(operation);
        HttpMethod = GetHttpMethod(operation);

        if (!operation.TryGetRouteHandlerPattern(out var routeToken))
        {
            Diagnostics.Add(DiagnosticDescriptors.UnableToResolveRoutePattern);
            return;
        }

        RoutePattern = routeToken.ValueText;

        if (!operation.TryGetRouteHandlerMethod(out var method))
        {
            Diagnostics.Add(DiagnosticDescriptors.UnableToResolveMethod);
            return;
        }

        Response = new EndpointResponse(method, wellKnownTypes);
        IsAwaitable = Response.IsAwaitable;

        if (method.Parameters.Length == 0)
        {
            return;
        }

        var parameters = new EndpointParameter[method.Parameters.Length];

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = new EndpointParameter(method.Parameters[i], wellKnownTypes);

            if (parameter.Source == EndpointParameterSource.Unknown)
            {
                Diagnostics.Add(DiagnosticDescriptors.GetUnableToResolveParameterDescriptor(parameter.Name));
                return;
            }

            parameters[i] = parameter;
        }

        Parameters = parameters;
        IsAwaitable |= parameters.Any(parameter => parameter.Source == EndpointParameterSource.JsonBody);
    }

    public string HttpMethod { get; }
    public bool IsAwaitable { get; set; }
    public string? RoutePattern { get; }
    public EndpointResponse? Response { get; }
    public EndpointParameter[] Parameters { get; } = Array.Empty<EndpointParameter>();
    public string EmitArgumentList() => _argumentListCache ??= string.Join(", ", Parameters.Select(p => p.EmitArgument()));

    public List<DiagnosticDescriptor> Diagnostics { get; } = new List<DiagnosticDescriptor>();

    public (string File, int LineNumber) Location { get; }
    public IInvocationOperation Operation { get; }

    public override bool Equals(object o) =>
        o is Endpoint other && Location == other.Location && SignatureEquals(this, other);

    public override int GetHashCode() =>
        HashCode.Combine(Location, GetSignatureHashCode(this));

    public static bool SignatureEquals(Endpoint a, Endpoint b)
    {
        if (!a.Response.WrappedResponseType.Equals(b.Response.WrappedResponseType, StringComparison.Ordinal) ||
            !a.HttpMethod.Equals(b.HttpMethod, StringComparison.Ordinal) ||
            a.Parameters.Length != b.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Parameters.Length; i++)
        {
            if (!a.Parameters[i].Equals(b.Parameters[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static int GetSignatureHashCode(Endpoint endpoint)
    {
        var hashCode = new HashCode();
        hashCode.Add(endpoint.Response.WrappedResponseType);
        hashCode.Add(endpoint.HttpMethod);

        foreach (var parameter in endpoint.Parameters)
        {
            hashCode.Add(parameter);
        }

        return hashCode.ToHashCode();
    }

    private static (string, int) GetLocation(IInvocationOperation operation)
    {
        var filePath = operation.Syntax.SyntaxTree.FilePath;
        var span = operation.Syntax.SyntaxTree.GetLineSpan(operation.Syntax.Span);
        var lineNumber = span.StartLinePosition.Line + 1;
        return (filePath, lineNumber);
    }

    private static string GetHttpMethod(IInvocationOperation operation)
    {
        var syntax = (InvocationExpressionSyntax)operation.Syntax;
        var expression = (MemberAccessExpressionSyntax)syntax.Expression;
        var name = (IdentifierNameSyntax)expression.Name;
        var identifier = name.Identifier;
        return identifier.ValueText;
    }
}
