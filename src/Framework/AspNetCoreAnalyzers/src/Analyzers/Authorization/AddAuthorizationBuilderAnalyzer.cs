// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        if (policyServiceCollectionExtensions is null)
        {
            return;
        }

        var addAuthorizationMethod = policyServiceCollectionExtensions.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(member => member is { Name: "AddAuthorization", Parameters.Length: 2 });

        if (addAuthorizationMethod is null)
        {
            return;
        }

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, addAuthorizationMethod)
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, policyServiceCollectionExtensions)
                && IsLastCallInChain(invocation)
                && IsCompatibleWithAuthorizationBuilder(invocation, authorizationOptionsTypes))
            {
                AddDiagnosticInformation(context, invocation.Syntax.GetLocation());
            }

        }, OperationKind.Invocation);
    }

    private static bool IsCompatibleWithAuthorizationBuilder(IInvocationOperation invocation, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        if (TryGetConfigureArgumentOperation(invocation, out var configureArgumentOperation)
            && TryGetConfigureDelegateCreationOperation(configureArgumentOperation, out var configureDelegateCreationOperation)
            && TryGetConfigureAnonymousFunctionOperation(configureDelegateCreationOperation, out var configureAnonymousFunctionOperation)
            && TryGetConfigureBlockOperation(configureAnonymousFunctionOperation, out var configureBlockOperation))
        {
            // Ensure that the child operations of the configuration action passed to AddAuthorization are all related to AuthorizationOptions.
            var allOperationsInvolveAuthorizationOptions = configureBlockOperation.ChildOperations
                .Where(operation => operation is not IReturnOperation { IsImplicit: true })
                .All(operation => DoesOperationInvolveAuthorizationOptions(operation, authorizationOptionsTypes));

            return allOperationsInvolveAuthorizationOptions
                // Ensure that the configuration action passed to AddAuthorization does not use any AuthorizationOptions-specific APIs.
                && IsConfigureActionCompatibleWithAuthorizationBuilder(configureBlockOperation, authorizationOptionsTypes);
        }

        return false;
    }

    private static bool TryGetConfigureArgumentOperation(IInvocationOperation invocation, [NotNullWhen(true)] out IArgumentOperation? configureArgumentOperation)
    {
        configureArgumentOperation = null;

        if (invocation is { Arguments: { Length: 2 } invocationArguments })
        {
            configureArgumentOperation = invocationArguments[1];
            return true;
        }

        return false;
    }

    private static bool TryGetConfigureDelegateCreationOperation(IArgumentOperation configureArgumentOperation, [NotNullWhen(true)] out IDelegateCreationOperation? configureDelegateCreationOperation)
    {
        configureDelegateCreationOperation = null;

        if (configureArgumentOperation is { ChildOperations: { Count: 1 } argumentChildOperations }
            && argumentChildOperations.First() is IDelegateCreationOperation delegateCreationOperation)
        {
            configureDelegateCreationOperation = delegateCreationOperation;
            return true;
        }

        return false;
    }

    private static bool TryGetConfigureAnonymousFunctionOperation(IDelegateCreationOperation configureDelegateCreationOperation, [NotNullWhen(true)] out IAnonymousFunctionOperation? configureAnonymousFunctionOperation)
    {
        configureAnonymousFunctionOperation = null;

        if (configureDelegateCreationOperation is { ChildOperations: { Count: 1 } delegateCreationChildOperations }
            && delegateCreationChildOperations.First() is IAnonymousFunctionOperation anonymousFunctionOperation)
        {
            configureAnonymousFunctionOperation = anonymousFunctionOperation;
            return true;
        }

        return false;
    }

    private static bool TryGetConfigureBlockOperation(IAnonymousFunctionOperation configureAnonymousFunctionOperation, [NotNullWhen(true)] out IBlockOperation? configureBlockOperation)
    {
        configureBlockOperation = null;

        if (configureAnonymousFunctionOperation is { ChildOperations: { Count: 1 } anonymousFunctionChildOperations }
            && anonymousFunctionChildOperations.First() is IBlockOperation blockOperation)
        {
            configureBlockOperation = blockOperation;
            return true;
        }

        return false;
    }

    private static bool DoesOperationInvolveAuthorizationOptions(IOperation operation, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        if (operation is IExpressionStatementOperation { Operation: { } expressionStatementOperation })
        {
            if (expressionStatementOperation is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.ContainingType: { } propertyReferenceContainingType } }
                && SymbolEqualityComparer.Default.Equals(propertyReferenceContainingType, authorizationOptionsTypes.AuthorizationOptions))
            {
                return true;
            }

            if (expressionStatementOperation is IInvocationOperation { TargetMethod.ContainingType: { } invokedMethodContainingType }
                && SymbolEqualityComparer.Default.Equals(invokedMethodContainingType, authorizationOptionsTypes.AuthorizationOptions))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConfigureActionCompatibleWithAuthorizationBuilder(IBlockOperation configureAction, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        var usesAuthorizationOptionsSpecificAPIs = configureAction.Descendants()
            .Any(operation => UsesAuthorizationOptionsSpecificGetters(operation, authorizationOptionsTypes)
                || UsesAuthorizationOptionsGetPolicy(operation, authorizationOptionsTypes));

        return !usesAuthorizationOptionsSpecificAPIs;
    }

    private static bool UsesAuthorizationOptionsSpecificGetters(IOperation operation, AuthorizationOptionsTypes authorizationOptionsTypes)
    {
        if (operation is IPropertyReferenceOperation propertyReferenceOperation)
        {
            var property = propertyReferenceOperation.Property;

            // Check that the referenced property is not being set.
            if (propertyReferenceOperation.Parent is IAssignmentOperation { Target: IPropertyReferenceOperation targetProperty }
                && SymbolEqualityComparer.Default.Equals(property, targetProperty.Property))
            {
                // Ensure the referenced property isn't being assigned to itself
                // (i.e. options.DefaultPolicy = options.DefaultPolicy;)
                if (propertyReferenceOperation.Parent is IAssignmentOperation { Value: IPropertyReferenceOperation valueProperty }
                    && SymbolEqualityComparer.Default.Equals(property, valueProperty.Property))
                {
                    return true;
                }

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
            && SymbolEqualityComparer.Default.Equals(methodReferenceOperation.Member.ContainingType, authorizationOptionsTypes.AuthorizationOptions))
        {
            return true;
        }

        if (operation is IInvocationOperation invocationOperation
            && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod, authorizationOptionsTypes.GetPolicy)
            && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ContainingType, authorizationOptionsTypes.AuthorizationOptions))
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
}
