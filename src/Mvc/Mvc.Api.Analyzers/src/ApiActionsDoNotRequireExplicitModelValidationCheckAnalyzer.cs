// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        ApiDiagnosticDescriptors.API1003_ApiActionsDoNotRequireExplicitModelValidationCheck);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (!ApiControllerSymbolCache.TryCreate(context.Compilation, out var symbolCache))
            {
                // No-op if we can't find types we care about.
                return;
            }

            InitializeWorker(context, symbolCache);
        });
    }

    private static void InitializeWorker(CompilationStartAnalysisContext context, ApiControllerSymbolCache symbolCache)
    {
        context.RegisterOperationAction(context =>
        {
            var ifOperation = (IConditionalOperation)context.Operation;

            if (ifOperation.WhenTrue == null || ifOperation.WhenFalse != null)
            {
                // We only support expressions of the format
                // if (!ModelState.IsValid)
                // or
                // if (ModelState.IsValid == false)
                // If the conditional is missing a true condition or has an else expression, skip this operation.
                return;
            }

            var parent = ifOperation.Parent;
            if (parent == null)
            {
                // No parent, nothing to do
                return;
            }

            if (parent.Kind == OperationKind.Block && parent.Parent != null)
            {
                parent = parent.Parent;
            }

            if (parent.Kind != OperationKind.MethodBodyOperation)
            {
                // Only support top-level ModelState IsValid checks.
                return;
            }

            var trueStatement = UnwrapSingleStatementBlock(ifOperation.WhenTrue);
            if (trueStatement.Kind != OperationKind.Return)
            {
                // We need to verify that the if statement does a ModelState.IsValid check and that the block inside contains
                // a single return statement returning a 400. We'l get to it in just a bit
                return;
            }

            if (context.ContainingSymbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (!ApiControllerFacts.IsApiControllerAction(symbolCache, methodSymbol))
            {
                // Not a ApiController. Nothing to do here.
                return;
            }

            if (!IsModelStateIsValidCheck(symbolCache, ifOperation.Condition))
            {
                return;
            }

            var returnOperation = (IReturnOperation)trueStatement;

            var returnValue = returnOperation.ReturnedValue;
            if (returnValue == null ||
                !symbolCache.IActionResult.IsAssignableFrom(returnValue.Type))
            {
                return;
            }

            var actualMetadata = ActualApiResponseMetadataFactory.InspectReturnOperation(
                in symbolCache,
                returnOperation);

            foreach (var metadata in actualMetadata)
            {
                if (!metadata.HasValue || metadata.Value.StatusCode != 400)
                {
                    return;
                }
            }

            var returnStatementSyntax = returnOperation.Syntax;
            var additionalLocations = new[]
            {
                    ifOperation.Syntax.GetLocation(),
                    returnStatementSyntax.GetLocation(),
            };

            context.ReportDiagnostic(
                Diagnostic.Create(
                    ApiDiagnosticDescriptors.API1003_ApiActionsDoNotRequireExplicitModelValidationCheck,
                    ifOperation.Syntax.GetLocation(),
                    additionalLocations: additionalLocations));
        }, OperationKind.Conditional);
    }

    private static bool IsModelStateIsValidCheck(in ApiControllerSymbolCache symbolCache, IOperation condition)
    {
        switch (condition.Kind)
        {
            case OperationKind.UnaryOperator:
                var operation = ((IUnaryOperation)condition).Operand;
                return IsModelStateIsValidPropertyAccessor(symbolCache, operation);

            case OperationKind.BinaryOperator:
                var binaryOperation = (IBinaryOperation)condition;
                if (binaryOperation.OperatorKind == BinaryOperatorKind.Equals)
                {
                    // (ModelState.IsValid == false) OR (false == ModelState.IsValid)
                    return EvaluateBinaryOperator(symbolCache, binaryOperation.LeftOperand, binaryOperation.RightOperand, false) ||
                        EvaluateBinaryOperator(symbolCache, binaryOperation.RightOperand, binaryOperation.LeftOperand, false);
                }
                else if (binaryOperation.OperatorKind == BinaryOperatorKind.NotEquals)
                {
                    // (ModelState.IsValid != true) OR (true != ModelState.IsValid)
                    return EvaluateBinaryOperator(symbolCache, binaryOperation.LeftOperand, binaryOperation.RightOperand, true) ||
                        EvaluateBinaryOperator(symbolCache, binaryOperation.RightOperand, binaryOperation.LeftOperand, true);
                }
                return false;

            default:
                return false;
        }
    }

    private static bool EvaluateBinaryOperator(
        in ApiControllerSymbolCache symbolCache,
        IOperation operation,
        IOperation otherOperation,
        bool expectedConstantValue)
    {
        if (operation.Kind != OperationKind.Literal)
        {
            return false;
        }

        var constantValue = ((ILiteralOperation)operation).ConstantValue;
        if (!constantValue.HasValue ||
            !(constantValue.Value is bool boolConstantValue) ||
            boolConstantValue != expectedConstantValue)
        {
            return false;
        }

        return IsModelStateIsValidPropertyAccessor(symbolCache, otherOperation);
    }

    private static bool IsModelStateIsValidPropertyAccessor(in ApiControllerSymbolCache symbolCache, IOperation operation)
    {
        if (operation.Kind != OperationKind.PropertyReference)
        {
            return false;
        }

        var propertyReference = (IPropertyReferenceOperation)operation;
        if (propertyReference.Property.Name != "IsValid")
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(propertyReference.Member.ContainingType, symbolCache.ModelStateDictionary))
        {
            return false;
        }

        if (propertyReference.Instance?.Kind != OperationKind.PropertyReference)
        {
            // Verify this is referring to the ModelState property on the current controller instance
            return false;
        }

        var modelStatePropertyReference = (IPropertyReferenceOperation)propertyReference.Instance;
        if (modelStatePropertyReference.Instance?.Kind != OperationKind.InstanceReference)
        {
            return false;
        }

        return true;
    }

    private static IOperation UnwrapSingleStatementBlock(IOperation statement)
    {
        return statement is IBlockOperation block && block.Operations.Length == 1 ?
            block.Operations[0] :
            statement;
    }
}
