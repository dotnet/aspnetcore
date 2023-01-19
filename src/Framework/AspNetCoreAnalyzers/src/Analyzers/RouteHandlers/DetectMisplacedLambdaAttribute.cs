// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectMisplacedLambdaAttribute(
        in OperationAnalysisContext context,
        IAnonymousFunctionOperation lambda)
    {
        // This analyzer will only process invocations that are immediate children of the
        // AnonymousFunctionOperation provided as the delegate endpoint. We'll support checking
        // for misplaced attributes in `() => Hello()` and `() => { return Hello(); }` but not in:
        // () => {
        //    Hello();
        //    return "foo";
        // }

        // All lambdas have a single child which is a BlockOperation. We search ChildOperations for
        // the invocation expression.
        if (lambda.ChildOperations.Count != 1 || lambda.ChildOperations.FirstOrDefault() is not IBlockOperation blockOperation)
        {
            Debug.Fail("Expected a single top-level BlockOperation for all lambdas.");
            return;
        }

        // If no method definition was found for the lambda, then abort.
        if (GetReturnedInvocation(blockOperation) is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes();
        var location = lambda.Syntax.GetLocation();

        foreach (var attribute in attributes)
        {
            if (IsInValidNamespace(attribute.AttributeClass?.ContainingNamespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DetectMisplacedLambdaAttribute,
                    location,
                    attribute.AttributeClass?.Name,
                    methodSymbol.Name));
            }
        }

        static bool IsInValidNamespace(INamespaceSymbol? @namespace)
        {
            if (@namespace != null && !@namespace.IsGlobalNamespace)
            {
                // Check for Microsoft.AspNetCore in the ContainingNamespaces for this type
                if (@namespace.Name.Equals("AspNetCore", System.StringComparison.Ordinal) && @namespace.ContainingNamespace.Name.Equals("Microsoft", System.StringComparison.Ordinal))
                {
                    return true;
                }

                return IsInValidNamespace(@namespace.ContainingNamespace);
            }

            return false;
        }

        static IMethodSymbol? GetReturnedInvocation(IBlockOperation blockOperation)
        {
            foreach (var op in blockOperation.ChildOperations.Reverse())
            {
                if (op is IReturnOperation returnStatement)
                {
                    if (returnStatement.ReturnedValue is IInvocationOperation invocationReturn)
                    {
                        return invocationReturn.TargetMethod;
                    }

                    // Sometimes expression backed lambdas include an IReturnOperation with a null ReturnedValue
                    // right after the IExpressionStatementOperation whose Operation is the real ReturnedValue,
                    // so we keep looking backwards rather than returning null immediately.
                }
                else if (op is IExpressionStatementOperation expression)
                {
                    if (expression.Operation is IInvocationOperation invocationExpression)
                    {
                        return invocationExpression.TargetMethod;
                    }

                    return null;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }
    }
}
