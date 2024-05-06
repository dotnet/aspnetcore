// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal class Endpoint
{
    public Endpoint(IInvocationOperation operation, WellKnownTypes wellKnownTypes, SemanticModel semanticModel)
    {
        Operation = operation;
        Location = GetLocation(operation);
#pragma warning disable RSEXPERIMENTAL002 // Experimental interceptable location API
        InterceptableLocation = semanticModel.GetInterceptableLocation((InvocationExpressionSyntax)operation.Syntax)!;
#pragma warning restore RSEXPERIMENTAL002
        HttpMethod = GetHttpMethod(operation);
        EmitterContext = new EmitterContext();

        if (!operation.TryGetRouteHandlerMethod(semanticModel, out var method))
        {
            Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveMethod, Operation.Syntax.GetLocation()));
            return;
        }

        Response = new EndpointResponse(method, wellKnownTypes);
        Response.EmitRequiredDiagnostics(Diagnostics, Operation.Syntax.GetLocation());
        IsAwaitable = Response?.IsAwaitable == true;

        EmitterContext.HasResponseMetadata = Response is { } response && !(response.IsIResult || response.HasNoResponse);

        // NOTE: We set this twice. It is possible that we don't have any parameters so we
        //       want this to be true if the response type implements IEndpointMetadataProvider.
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
            var parameterSymbol = method.Parameters[i];
            parameterSymbol.EmitRequiredDiagnostics(Diagnostics, Operation.Syntax.GetLocation());
            if (Diagnostics.Count > 0)
            {
                continue;
            }
            var parameter = new EndpointParameter(this, parameterSymbol, wellKnownTypes);

            switch (parameter.Source)
            {
                case EndpointParameterSource.BindAsync:
                    switch (parameter.BindMethod)
                    {
                        case BindabilityMethod.IBindableFromHttpContext:
                        case BindabilityMethod.BindAsyncWithParameter:
                            NeedsParameterArray = true;
                            break;
                    }
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

        EmitterContext.RequiresLoggingHelper = !Parameters.All(parameter =>
            parameter is not null &&
            parameter.Source == EndpointParameterSource.SpecialType ||
            parameter is { IsArray: true, ElementType.SpecialType: SpecialType.System_String, Source: EndpointParameterSource.Query });
    }

    public string HttpMethod { get; }
    public bool IsAwaitable { get; set; }
    public bool NeedsParameterArray { get; }
    public string? RoutePattern { get; }
    public EmitterContext EmitterContext { get; }
    public EndpointResponse? Response { get; }
    public EndpointParameter[] Parameters { get; } = Array.Empty<EndpointParameter>();
    public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

    public (string File, int LineNumber, int CharacterNumber) Location { get; }
#pragma warning disable RSEXPERIMENTAL002 // Experimental interceptable location API
    public InterceptableLocation InterceptableLocation { get; }
#pragma warning restore RSEXPERIMENTAL002
    public IInvocationOperation Operation { get; }

    public override bool Equals(object o) =>
        o is Endpoint other && InterceptableLocation == other.InterceptableLocation && SignatureEquals(this, other);

    public override int GetHashCode() =>
        HashCode.Combine(Location, GetSignatureHashCode(this));

    public static bool SignatureEquals(Endpoint a, Endpoint b)
    {
        if (!string.Equals(a.Response?.WrappedResponseTypeDisplayName, b.Response?.WrappedResponseTypeDisplayName, StringComparison.Ordinal) ||
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
        hashCode.Add(endpoint.Response?.WrappedResponseTypeDisplayName);
        hashCode.Add(endpoint.HttpMethod);

        foreach (var parameter in endpoint.Parameters)
        {
            hashCode.Add(parameter.Type, SymbolEqualityComparer.Default);
        }

        return hashCode.ToHashCode();
    }

    private static (string, int, int) GetLocation(IInvocationOperation operation)
    {
        // The invocation expression consists of two properties:
        // - Expression: which is a `MemberAccessExpressionSyntax` that represents the method being invoked.
        // - ArgumentList: the list of arguments being invoked.
        // Here, we resolve the `MemberAccessExpressionSyntax` to get the location of the method being invoked.
        var memberAccessorExpression = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)operation.Syntax).Expression);
        // The `MemberAccessExpressionSyntax` in turn includes three properties:
        // - Expression: the expression that is being accessed.
        // - OperatorToken: the operator token, typically the dot separate.
        // - Name: the name of the member being accessed, typically `MapGet` or `MapPost`, etc.
        // Here, we resolve the `Name` to extract the location of the method being invoked.
        var invocationNameSpan = memberAccessorExpression.Name.Span;
        // Resolve LineSpan associated with the name span so we can resolve the line and character number.
        var lineSpan = operation.Syntax.SyntaxTree.GetLineSpan(invocationNameSpan);
        // Resolve the filepath of the invocation while accounting for source mapped paths.
        var filePath = operation.Syntax.SyntaxTree.GetInterceptorFilePath(operation.SemanticModel?.Compilation.Options.SourceReferenceResolver);
        // LineSpan.LinePosition is 0-indexed, but we want to display 1-indexed line and character numbers in the interceptor attribute.
        return (filePath, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1);
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
