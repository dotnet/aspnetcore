// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal static class DiagnosticEmitter
{
    public static void EmitRequiredDiagnostics(this EndpointResponse response, List<Diagnostic> diagnostics, Location location)
    {
        if (response.ResponseType is ITypeParameterSymbol)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeParametersNotSupported, location));
        }

        if (response.ResponseType?.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InaccessibleTypesNotSupported, location));
        }

        if (response.ResponseType?.IsAnonymousType == true)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveAnonymousReturnType, location));
        }
    }

    public static void EmitRequiredDiagnostics(this IParameterSymbol parameterSymbol, List<Diagnostic> diagnostics, Location location)
    {
        var typeSymbol = parameterSymbol.Type;
        if (typeSymbol is ITypeParameterSymbol ||
            typeSymbol is INamedTypeSymbol &&
            ((INamedTypeSymbol)typeSymbol).TypeArguments.Any(a => a is ITypeParameterSymbol))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeParametersNotSupported, location));
        }

        if (typeSymbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected ||
            typeSymbol is INamedTypeSymbol &&
            ((INamedTypeSymbol)typeSymbol).TypeArguments.Any(typeArg =>
                typeArg.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InaccessibleTypesNotSupported, location, typeSymbol.Name));
        }
    }
}
