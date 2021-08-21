// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public partial class DelegateEndpointAnalyzer : DiagnosticAnalyzer
{
    private static void DetectMisplacedLambdaAttribute(
        in OperationAnalysisContext context,
        IInvocationOperation invocation,
        IAnonymousFunctionOperation lambda)
    {
        // This analyzer will only process invocations that are immediate children of the
        // AnonymousFunctionOperation provided as the delegate endpoint. We'll support checking
        // for misplaced attributes in `() => Hello()` and `() => { return Hello(); }` but not in:
        // () => {
        //    Hello();
        //    return "foo";
        // }
        InvocationExpressionSyntax? targetInvocation = null;

        // () => Hello() has a single child which is a BlockOperation so we check to see if
        // expression associated with that operation is an invocation expression
        if (lambda.Children.First().Syntax is InvocationExpressionSyntax innerInvocation)
        {
            targetInvocation = innerInvocation;
        }

        if (lambda.Children.First().Children.First() is IReturnOperation returnOperation
            && returnOperation.ReturnedValue is IInvocationOperation returnedInvocation)

        {
            targetInvocation = (InvocationExpressionSyntax)returnedInvocation.Syntax;
        }

        if (targetInvocation is null)
        {
            return;
        }


        var methodOperation = invocation.SemanticModel.GetSymbolInfo(targetInvocation);

        var attributes = methodOperation.Symbol.GetAttributes();
        var location = lambda.Syntax.GetLocation();

        foreach (var attribute in attributes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DetectMisplacedLambdaAttribute,
                location,
                attribute.AttributeClass?.Name,
                methodOperation.Symbol.Name));
        }
    }
}