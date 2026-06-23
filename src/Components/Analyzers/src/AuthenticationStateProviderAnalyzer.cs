// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AuthenticationStateProviderAnalyzer : DiagnosticAnalyzer
{
    public AuthenticationStateProviderAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.AuthenticationStateProviderCachedWithoutSubscription);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var authStateProviderType = context.Compilation.GetTypeByMetadataName(ComponentsApi.AuthenticationStateProvider.MetadataName);
            if (authStateProviderType is null)
            {
                return;
            }

            context.RegisterSymbolStartAction(context =>
            {
                var namedType = (INamedTypeSymbol)context.Symbol;

                // Only analyze types that have access to an AuthenticationStateProvider
                // (either through a field/property or by inheriting from it)
                if (!TypeUsesAuthenticationStateProvider(namedType, authStateProviderType))
                {
                    return;
                }

                var hasGetAuthStateCall = false;
                var hasAuthStateChangedSubscription = false;

                context.RegisterOperationAction(operationContext =>
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    if (invocation.Instance is not null &&
                        IsAuthenticationStateProviderType(invocation.Instance.Type, authStateProviderType) &&
                        invocation.TargetMethod.Name == ComponentsApi.AuthenticationStateProvider.GetAuthenticationStateAsync)
                    {
                        hasGetAuthStateCall = true;
                    }
                }, OperationKind.Invocation);

                context.RegisterOperationAction(operationContext =>
                {
                    var eventAssignment = (IEventAssignmentOperation)operationContext.Operation;
                    if (eventAssignment.EventReference is IEventReferenceOperation eventRef &&
                        IsAuthenticationStateProviderType(eventRef.Instance?.Type, authStateProviderType) &&
                        eventRef.Event.Name == ComponentsApi.AuthenticationStateProvider.AuthenticationStateChanged)
                    {
                        hasAuthStateChangedSubscription = true;
                    }
                }, OperationKind.EventAssignment);

                context.RegisterSymbolEndAction(endContext =>
                {
                    if (hasGetAuthStateCall && !hasAuthStateChangedSubscription)
                    {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.AuthenticationStateProviderCachedWithoutSubscription,
                            namedType.Locations.FirstOrDefault(),
                            namedType.Name));
                    }
                });
            }, SymbolKind.NamedType);
        });
    }

    private static bool TypeUsesAuthenticationStateProvider(INamedTypeSymbol type, INamedTypeSymbol authStateProviderType)
    {
        // Check if the type itself derives from AuthenticationStateProvider
        if (IsAuthenticationStateProviderType(type, authStateProviderType))
        {
            return true;
        }

        // Check if any field or property is of type AuthenticationStateProvider
        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol field && IsAuthenticationStateProviderType(field.Type, authStateProviderType))
            {
                return true;
            }

            if (member is IPropertySymbol property && IsAuthenticationStateProviderType(property.Type, authStateProviderType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAuthenticationStateProviderType(ITypeSymbol? type, INamedTypeSymbol authStateProviderType)
    {
        if (type is null)
        {
            return false;
        }

        // Check if the type is AuthenticationStateProvider or derives from it
        var current = type;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, authStateProviderType))
            {
                return true;
            }
            current = current.BaseType;
        }

        return false;
    }
}
