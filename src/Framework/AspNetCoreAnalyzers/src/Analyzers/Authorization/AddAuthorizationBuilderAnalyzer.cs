// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Authorization;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddAuthorizationBuilderAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseAddAuthorizationBuilder);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.Compilation);

        var policyServiceCollectionExtensions = wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_DependencyInjection_PolicyServiceCollectionExtensions);
        var addAuthorizationMethod = policyServiceCollectionExtensions.GetMembers()
            .OfType<IMethodSymbol>()
            .Single(member => member.Parameters.Length == 2 && member.Name == "AddAuthorization");

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (invocation.TargetMethod.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, policyServiceCollectionExtensions)
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, addAuthorizationMethod)
                && IsConfigureActionCompatibleWithAuthorizationBuilder(invocation, wellKnownTypes))
            {
                AddDiagnosticInformation(context, invocation.Syntax.GetLocation());
            }

        }, OperationKind.Invocation);
    }

    private static bool IsConfigureActionCompatibleWithAuthorizationBuilder(IInvocationOperation invocation, WellKnownTypes wellKnownTypes)
    {
        if (invocation.Arguments is not { Length: 2 })
        {
            return false;
        }

        var configureAction = invocation.Arguments[1];

        var authorizationOptions = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_AuthorizationOptions);
        var authorizationOptionsMembers = authorizationOptions.GetMembers();

        var defaultPolicyProperty = authorizationOptionsMembers.First(member =>
            member.Kind == SymbolKind.Property && member.Name == "DefaultPolicy");

        var fallbackPolicyProperty = authorizationOptionsMembers.First(member =>
            member.Kind == SymbolKind.Property && member.Name == "FallbackPolicy");

        var invokeHandlersAfterFailureProperty = authorizationOptionsMembers.First(member =>
            member.Kind == SymbolKind.Property && member.Name == "InvokeHandlersAfterFailure");

        var getPolicyMethod = authorizationOptionsMembers.First(member =>
            member.Kind == SymbolKind.Method && member.Name == "GetPolicy");

        return !configureAction.Descendants().Any(operation =>
        {
            if (operation is IPropertyReferenceOperation propertyReferenceOperation)
            {
                var property = propertyReferenceOperation.Property;

                if (SymbolEqualityComparer.Default.Equals(property, defaultPolicyProperty)
                    || SymbolEqualityComparer.Default.Equals(property, fallbackPolicyProperty)
                    || SymbolEqualityComparer.Default.Equals(property, invokeHandlersAfterFailureProperty))
                {
                    return true;
                }

                return false;
            }

            if (operation is IMethodReferenceOperation methodReferenceOperation)
            {
                if (SymbolEqualityComparer.Default.Equals(methodReferenceOperation.Member, getPolicyMethod)
                    && SymbolEqualityComparer.Default.Equals(methodReferenceOperation.Member.ContainingSymbol, authorizationOptions))
                {
                    return true;
                }

                return false;
            }

            if (operation is IInvocationOperation invocationOperation)
            {
                if (SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ContainingType, authorizationOptions)
                    && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod, getPolicyMethod))
                {
                    return true;
                }

                return false;
            }

            return false;
        });
    }

    private static void AddDiagnosticInformation(OperationAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseAddAuthorizationBuilder,
            location));
    }
}
