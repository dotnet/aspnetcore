// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal class Endpoint
{
    public Endpoint(IInvocationOperation operation, WellKnownTypes wellKnownTypes, SemanticModel semanticModel)
    {
        Operation = operation;
        Location = GetLocation(operation);
        HttpMethod = GetHttpMethod(operation);
        EmitterContext = new EmitterContext();

        if (!operation.TryGetRouteHandlerPattern(out var routeToken))
        {
            Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveRoutePattern, Operation.Syntax.GetLocation()));
            return;
        }

        RoutePattern = routeToken;

        if (!operation.TryGetRouteHandlerMethod(semanticModel, out var method) || method == null)
        {
            Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveMethod, Operation.Syntax.GetLocation()));
            return;
        }

        Response = new EndpointResponse(method, wellKnownTypes);
        if (Response.IsAnonymousType)
        {
            Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveAnonymousReturnType, Operation.Syntax.GetLocation()));
            return;
        }

        EmitterContext.RequiresMetadataHelperTypes = !(Response.IsIResult || Response.HasNoResponse);
        EmitterContext.HasJsonResponse = Response is not { ResponseType: { IsSealed: true } or { IsValueType: true } };
        IsAwaitable = Response?.IsAwaitable == true;

        // NOTE: We set this twice. It is possible that we don't have any parameters so we
        //       want this to be true if the reponse type implements IEndpointMetadataProvider.
        //       Later on we set this to be true if the parameters or the response type
        //       implement the interface.
        EmitterContext.HasEndpointMetadataProvider = Response!.IsEndpointMetadataProvider;

        if (method.Parameters.Length == 0)
        {
            EmitterContext.RequiresLoggingHelper = false;
            return;
        }

        var parameters = new EndpointParameter[method.Parameters.Length];

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = new EndpointParameter(this, method.Parameters[i], wellKnownTypes);

            switch (parameter.Source)
            {
                case EndpointParameterSource.BindAsync:
                    IsAwaitable = true;
                    switch (parameter.BindMethod)
                    {
                        case BindabilityMethod.IBindableFromHttpContext:
                        case BindabilityMethod.BindAsyncWithParameter:
                            NeedsParameterArray = true;
                            break;
                    }
                    break;
                case EndpointParameterSource.JsonBody:
                case EndpointParameterSource.JsonBodyOrService:
                case EndpointParameterSource.FormBody:
                    IsAwaitable = true;
                    break;
                case EndpointParameterSource.Unknown:
                    Diagnostics.Add(Diagnostic.Create(
                        DiagnosticDescriptors.UnableToResolveParameterDescriptor,
                        Operation.Syntax.GetLocation(),
                        parameter.SymbolName));
                    break;
            }

            parameters[i] = parameter;
        }

        Parameters = parameters;

        EmitterContext.HasEndpointParameterMetadataProvider = Parameters.Any(p => p.IsEndpointParameterMetadataProvider);
        EmitterContext.HasEndpointMetadataProvider = Response!.IsEndpointMetadataProvider || Parameters.Any(p => p.IsEndpointMetadataProvider || p.IsEndpointParameterMetadataProvider);

        EmitterContext.HasJsonBodyOrService = Parameters.Any(parameter => parameter.Source == EndpointParameterSource.JsonBodyOrService);
        EmitterContext.HasJsonBody = Parameters.Any(parameter => parameter.Source == EndpointParameterSource.JsonBody);
        EmitterContext.HasFormBody = Parameters.Any(parameter => parameter.Source == EndpointParameterSource.FormBody);
        EmitterContext.HasRouteOrQuery = Parameters.Any(parameter => parameter.Source == EndpointParameterSource.RouteOrQuery);
        EmitterContext.HasBindAsync = Parameters.Any(parameter => parameter.Source == EndpointParameterSource.BindAsync);
        EmitterContext.HasParsable = Parameters.Any(parameter => parameter.IsParsable);
        EmitterContext.RequiresLoggingHelper = !Parameters.All(parameter =>
            parameter.Source == EndpointParameterSource.SpecialType ||
            parameter is { IsArray: true, ElementType.SpecialType: SpecialType.System_String, Source: EndpointParameterSource.Query });
    }

    public string HttpMethod { get; }
    public bool IsAwaitable { get; }
    public bool NeedsParameterArray { get; }
    public string? RoutePattern { get; }
    public EmitterContext EmitterContext { get;  }
    public EndpointResponse? Response { get; }
    public EndpointParameter[] Parameters { get; } = Array.Empty<EndpointParameter>();
    public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

    public (string File, int LineNumber) Location { get; }
    public IInvocationOperation Operation { get; }

    public override bool Equals(object o) =>
        o is Endpoint other && Location == other.Location && SignatureEquals(this, other);

    public override int GetHashCode() =>
        HashCode.Combine(Location, GetSignatureHashCode(this));

    public static bool SignatureEquals(Endpoint a, Endpoint b)
    {
        if (!string.Equals(a.Response?.WrappedResponseType, b.Response?.WrappedResponseType, StringComparison.Ordinal) ||
            !a.HttpMethod.Equals(b.HttpMethod, StringComparison.Ordinal) ||
            a.Parameters.Length != b.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Parameters.Length; i++)
        {
            if (!a.Parameters[i].SignatureEquals(b.Parameters[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static int GetSignatureHashCode(Endpoint endpoint)
    {
        var hashCode = new HashCode();
        hashCode.Add(endpoint.Response?.WrappedResponseType);
        hashCode.Add(endpoint.HttpMethod);

        foreach (var parameter in endpoint.Parameters)
        {
            hashCode.Add(parameter.Type, SymbolEqualityComparer.Default);
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
