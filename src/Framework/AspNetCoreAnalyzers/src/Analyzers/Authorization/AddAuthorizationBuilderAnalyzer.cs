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

        var authorizationOptionsTypes = new AuthorizationOptionsTypes(wellKnownTypes);
        if (!authorizationOptionsTypes.HasRequiredTypes)
        {
            return;
        }

        var policyServiceCollectionExtensions = wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_DependencyInjection_PolicyServiceCollectionExtensions);
        var addAuthorizationMethod = policyServiceCollectionExtensions.GetMembers()
            .OfType<IMethodSymbol>()
            .Single(member => member.Parameters.Length == 2 && member.Name == "AddAuthorization");

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (invocation.Arguments.Length == 2
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, policyServiceCollectionExtensions)
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, addAuthorizationMethod)
                && IsLastCallInChain(invocation)
                && IsConfigureActionCompatibleWithAuthorizationBuilder(invocation.Arguments[1], authorizationOptionsTypes))
            {
                AddDiagnosticInformation(context, invocation.Syntax.GetLocation());
            }

        }, OperationKind.Invocation);
    }

    private static bool IsConfigureActionCompatibleWithAuthorizationBuilder(IArgumentOperation configureAction, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        var usesAuthorizationOptionsSpecificAPIs = configureAction.Descendants()
            .Any(operation =>
                UsesAuthorizationOptionsSpecificGetters(operation, authorizationOptionsTypes)
                || UsesAuthorizationOptionsGetPolicy(operation, authorizationOptionsTypes));

        return !usesAuthorizationOptionsSpecificAPIs;
    }

    private static bool UsesAuthorizationOptionsSpecificGetters(IOperation operation, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        if (operation is IPropertyReferenceOperation propertyReferenceOperation)
        {
            var property = propertyReferenceOperation.Property;

            if (propertyReferenceOperation.Parent is IAssignmentOperation { Target: IPropertyReferenceOperation targetProperty } assignmentOperation
                && SymbolEqualityComparer.Default.Equals(property, targetProperty.Property))
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(property, authorizationOptionsTypes.DefaultPolicy)
                || SymbolEqualityComparer.Default.Equals(property, authorizationOptionsTypes.FallbackPolicy)
                || SymbolEqualityComparer.Default.Equals(property, authorizationOptionsTypes.InvokeHandlersAfterFailure))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesAuthorizationOptionsGetPolicy(IOperation operation, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        if (operation is IMethodReferenceOperation methodReferenceOperation
            && SymbolEqualityComparer.Default.Equals(methodReferenceOperation.Member, authorizationOptionsTypes.GetPolicy)
            && SymbolEqualityComparer.Default.Equals(methodReferenceOperation.Member.ContainingSymbol, authorizationOptionsTypes.AuthorizationOptions))
        {
            return true;
        }

        if (operation is IInvocationOperation invocationOperation
            && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod, authorizationOptionsTypes.GetPolicy)
            && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ContainingSymbol, authorizationOptionsTypes.AuthorizationOptions))
        {
            return true;
        }

        return false;
    }

    private static bool IsLastCallInChain(IInvocationOperation invocation)
    {
        return invocation.Parent is IExpressionStatementOperation;
    }

    private static void AddDiagnosticInformation(OperationAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseAddAuthorizationBuilder,
            location));
    }
    private sealed class AuthorizationOptionsTypes
    {
        public AuthorizationOptionsTypes(WellKnownTypes wellKnownTypes)
        {
            AuthorizationOptions = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_AuthorizationOptions);

            if (AuthorizationOptions is not null)
            {
                var authorizationOptionsMembers = AuthorizationOptions.GetMembers();

                var authorizationOptionsProperties = authorizationOptionsMembers.OfType<IPropertySymbol>();

                DefaultPolicy = authorizationOptionsProperties
                    .FirstOrDefault(member => member.Name == "DefaultPolicy");
                FallbackPolicy = authorizationOptionsProperties
                    .FirstOrDefault(member => member.Name == "FallbackPolicy");
                InvokeHandlersAfterFailure = authorizationOptionsProperties
                    .FirstOrDefault(member => member.Name == "InvokeHandlersAfterFailure");

                GetPolicy = authorizationOptionsMembers.OfType<IMethodSymbol>()
                    .FirstOrDefault(member => member.Name == "GetPolicy");
            }
        }

        public INamedTypeSymbol? AuthorizationOptions { get; }
        public IPropertySymbol? DefaultPolicy { get; }
        public IPropertySymbol? FallbackPolicy { get; }
        public IPropertySymbol? InvokeHandlersAfterFailure { get; }
        public IMethodSymbol? GetPolicy { get; }

        public bool HasRequiredTypes => AuthorizationOptions is not null
            && DefaultPolicy is not null
            && FallbackPolicy is not null
            && InvokeHandlersAfterFailure is not null
            && GetPolicy is not null;
    }
}
