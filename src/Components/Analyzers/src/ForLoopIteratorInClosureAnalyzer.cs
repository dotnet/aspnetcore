// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForLoopIteratorInClosureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(context =>
        {
            ForStatementSyntax forStatement = (ForStatementSyntax)context.Node;
            if (forStatement.Declaration is null || forStatement.Declaration.Variables.Count == 0)
            {
                // Nothing to analyze if there is no variable declaration in the for loop.
                return;
            }

            ForBlockState currentContextState = new ();
            AnalyzeForStatement(ref context, forStatement, ref currentContextState);
        }, SyntaxKind.ForStatement);
    }

    private static void AnalyzeForStatement(ref SyntaxNodeAnalysisContext context, ForStatementSyntax forStatement, ref ForBlockState parentForState)
    {
        // Get incremented on variables, since we are sure they can be problematic. Each incrementor should have one descendant identifier.
        IEnumerable<string> incrementorNames = forStatement.Incrementors.SelectMany(i => i.DescendantNodes().OfType<IdentifierNameSyntax>()).Select(v => v.Identifier.Text);
        IEnumerable<string> potentialNames = forStatement.Declaration.Variables.Select(v => v.Identifier.Text).Where(v => !incrementorNames.Contains(v));
        ForBlockState currentState = new(incrementorNames, potentialNames, parentForState);
        AnalyzeBlock(ref context, forStatement.Statement as BlockSyntax, ref currentState);
    }

    private static void AnalyzeBlock(ref SyntaxNodeAnalysisContext context, BlockSyntax blockStatement, ref ForBlockState forState)
    {
        if (blockStatement is null)
        {
            return;
        }

        for (var i = 0; i < blockStatement.Statements.Count; i++)
        {
            var statement = blockStatement.Statements[i];
            if (statement is ForStatementSyntax)
            {
                // We have a nested for loop. Handle it separately for additional variables to take into account.
                AnalyzeForStatement(ref context, statement as ForStatementSyntax, ref forState);
            }
            else if (statement.GetType().GetProperty("Statement")?.GetValue(statement) is BlockSyntax childBlockStatement)
            {
                // Other types of blocks just need to be analyzed recursively.
                AnalyzeBlock(ref context, childBlockStatement, ref forState);
            }

            ExpressionStatementSyntax currentExpStatement = statement as ExpressionStatementSyntax;
            InvocationExpressionSyntax currentInvocation = currentExpStatement?.Expression as InvocationExpressionSyntax;
            if (currentInvocation is null || !IsCallingBlazorBuilder(currentInvocation))
            {
                AnalyzeExpressionForIncremention(ref context, currentExpStatement, ref forState);

                // Other types of expressions are not related to Razor code generation, so we can skip them.
                continue;
            }

            bool insideElement = IsCallingMemberName(currentInvocation, "OpenElement");
            bool insideComponent = IsCallingMemberName(currentInvocation, "OpenComponent");
            if (insideElement || insideComponent)
            {
                do
                {
                    if (i + 1 >= blockStatement.Statements.Count)
                    {
                        break;
                    }

                    currentExpStatement = blockStatement.Statements[++i] as ExpressionStatementSyntax;
                    currentInvocation = currentExpStatement?.Expression as InvocationExpressionSyntax;
                    if (currentInvocation is null)
                    {
                        continue;
                    }

                    if (IsCallingMemberName(currentInvocation, "CloseElement") || IsCallingMemberName(currentInvocation, "CloseComponent"))
                    {
                        insideElement = insideComponent = false;
                        continue;
                    }

                    if (insideElement)
                    {
                        AnalyzeElementExpression(ref context, currentInvocation, ref forState);
                    }
                    else if (insideComponent)
                    {
                        AnalyzeComponentExpression(ref context, currentInvocation, ref forState);
                    }
                } while (insideElement || insideComponent);
            }
        }
    }

    private static bool IsCallingBlazorBuilder(InvocationExpressionSyntax invocation)
    {
        return (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            && (memberAccess.Expression is IdentifierNameSyntax identifierName)
            && identifierName.Identifier.Text == "__builder";
    }

    private static bool IsCallingMemberName(InvocationExpressionSyntax invocation, string checkName)
    {
        return ((invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            && memberAccess.Name.Identifier.Text == checkName);
    }

    private static bool IsAddingAttributeOfType(InvocationExpressionSyntax invocation, string checkTypeName)
    {
        if (!IsCallingMemberName(invocation, "AddAttribute") || invocation.ArgumentList.Arguments.Count < 3)
        {
            return false;
        }

        ExpressionSyntax attributeValueArgument = invocation.ArgumentList.Arguments[2].Expression;
        if (attributeValueArgument is CastExpressionSyntax castExpression)
        {
            return castExpression.Type is QualifiedNameSyntax typeName
                && typeName.Right.Identifier.Text == checkTypeName;
        }
        else if (attributeValueArgument is InvocationExpressionSyntax invocationExpression)
        {
            // Get the type from the immediate expression call. The arguments are stored separately, so we avoid unnecessary descending there yet, since we need only the type.
            var descendantNodes = invocationExpression.Expression.DescendantNodes(node => node is MemberAccessExpressionSyntax
                && node.GetText().ToString().Contains(checkTypeName));
            return descendantNodes.OfType<IdentifierNameSyntax>().Any(typeName => typeName.Identifier.Text == checkTypeName);
        }

        return false;
    }

    private static void AnalyzeElementExpression(ref SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, ref ForBlockState forState)
    {
        var variableNames = forState.GetAllVariableNames();
        if (IsAddingAttributeOfType(invocation, "EventCallback"))
        {
            var valueArgument = invocation.ArgumentList.Arguments[2].Expression;
            if (valueArgument is InvocationExpressionSyntax && IsCallingMemberName(valueArgument as InvocationExpressionSyntax, "CreateBinder"))
            {
                // Should have been already reported when calling the "BindConverter.FormatValue" method.
                return;
            }

            var lambdaExpressions = valueArgument.DescendantNodes(node => node is not LambdaExpressionSyntax).OfType<LambdaExpressionSyntax>();
            foreach (var lambdaExpression in lambdaExpressions)
            {
                var usedForVariables = lambdaExpression.Body.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Where(id => variableNames.Any(name => name == id.Identifier.Text));
                ReportUsedVariables(context, usedForVariables, ref forState);
            }
        }
        else if (IsAddingAttributeOfType(invocation, "BindConverter"))
        {
            // Use this method for better location accuracy. The EventCallback generated is only internally visible when using @bind.
            var usedForVariables = invocation.ArgumentList.Arguments[2].DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(id => variableNames.Any(name => name == id.Identifier.Text));
            ReportUsedVariables(context, usedForVariables, ref forState);
        }
    }

    private static void AnalyzeComponentExpression(ref SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, ref ForBlockState forState)
    {
        // Apply the same analysis as for elements, since components also have the same issue with capturing loop variables in attribute closures
        AnalyzeElementExpression(ref context, invocation, ref forState);

        // Check child content for using loop variables
        if (IsAddingAttributeOfType(invocation, "RenderFragment")
            || IsCallingMemberName(invocation, "AddComponentParameter"))
        {
            var variableNames = forState.GetAllVariableNames();
            // Now we can use plain descendant nodes, since we don't really care anymore for the exact structure anymore. Any usage of the loop variable in the content should cause issue.
            var usedForVariables = invocation.ArgumentList.Arguments[2].Expression.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(id => variableNames.Any(name => name == id.Identifier.Text));
            ReportUsedVariables(context, usedForVariables, ref forState);
        }
    }

    /// <summary>
    /// Check more simple expressions and assignments that alter loop variable not yet marked as incrementor.
    /// </summary>
    private static void AnalyzeExpressionForIncremention(ref SyntaxNodeAnalysisContext context, ExpressionStatementSyntax expression, ref ForBlockState forState)
    {
        if (expression == null)
        {
            return;
        }

        string variableName = string.Empty;
        if (expression.Expression is PostfixUnaryExpressionSyntax postfix
            && postfix.IsKind(SyntaxKind.PostIncrementExpression))
        {
            variableName = postfix.Operand.ToString();
        }
        else if (expression.Expression is PrefixUnaryExpressionSyntax prefix
            && prefix.IsKind(SyntaxKind.PreIncrementExpression))
        {
            variableName = prefix.Operand.ToString();
        }
        else if (expression.Expression is AssignmentExpressionSyntax assignment)
        {
            variableName = assignment.Left.ToString();
        }

        if (!string.IsNullOrEmpty(variableName))
        {
            forState.OnPotentialVariableIncremented(ref context, variableName);
        }
    }

    private static void ReportUsedVariables(SyntaxNodeAnalysisContext context, IEnumerable<IdentifierNameSyntax> usedVariables, ref ForBlockState forState)
    {
        foreach (var usedVariable in usedVariables)
        {
            if (forState.IncrementorNames.Any(name => name == usedVariable.Identifier.Text))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure,
                    usedVariable.GetLocation(),
                    usedVariable.Identifier.Text));
            }
            else
            {
                forState.AddOccurrence(usedVariable);
            }
        }
    }

    /// <summary>
    /// State class to keep track of variables that are being incremented or potentially incremented and their occurrences in the current for loop.
    /// Each nested for loop has its own state.
    /// </summary>
    private sealed class ForBlockState
    {
        // Names of variables that are being incremented on in the current context. Includes parent state variables.
        public List<string> IncrementorNames { get; set; }
        // Potential variable names that are not yet being incremented on which would not cause any issues so far. Includes parent state variables.
        public List<string> PotentialNames { get; set; }
        // Occurrences of potential variables that could be later on incremented. Not inherited from parent state.
        public List<IdentifierNameSyntax> PotentialVariableOccurrences { get; set; }

        public ForBlockState? ParentState { get; set; }
        public ForBlockState()
        {
            IncrementorNames = new List<string>();
            PotentialNames = new List<string>();
            PotentialVariableOccurrences = new List<IdentifierNameSyntax>();
        }

        public ForBlockState(IEnumerable<string> incrementorNames, IEnumerable<string> potentialNames, ForBlockState parentState)
        {
            ParentState = parentState;
            IncrementorNames = parentState.IncrementorNames.Concat(incrementorNames).ToList();
            PotentialNames = parentState.PotentialNames.Concat(potentialNames).ToList();
            PotentialVariableOccurrences = new List<IdentifierNameSyntax>();
        }

        public bool IsPotentialVariable(string variableName)
        {
            return PotentialNames.Any(name => name == variableName);
        }

        public IEnumerable<string> GetAllVariableNames()
        {
            return IncrementorNames.Concat(PotentialNames);
        }

        /// <summary>
        /// Add an occurrence of a potential variable. If the variable is later incremented on, call OnPotentialVariableIncremented.
        /// </summary>
        public void AddOccurrence(IdentifierNameSyntax identifier)
        {
            if (PotentialNames.Any(name => name == identifier.Identifier.Text))
            {
                PotentialVariableOccurrences.Add(identifier);
            }
        }

        /// <summary>
        /// When potential variable is incremented, move it to incremented variables and report diagnostics for all previous occurrences of it in the current context.
        /// Do this for all parent states as well, since the variable could be used in multiple nested contexts before being incremented.
        /// </summary>
        public void OnPotentialVariableIncremented(ref SyntaxNodeAnalysisContext context, string variableName)
        {
            IncrementorNames.Add(variableName);
            PotentialNames = PotentialNames.Where(name => name != variableName).ToList();
            List<IdentifierNameSyntax> unusedOccurrences = new List<IdentifierNameSyntax>();
            if (PotentialVariableOccurrences.Count > 0)
            {
                foreach (var usedVariable in PotentialVariableOccurrences)
                {
                    if (usedVariable.Identifier.Text == variableName)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure,
                            usedVariable.GetLocation(),
                            usedVariable.Identifier.Text));
                    }
                    else
                    {
                        unusedOccurrences.Add(usedVariable);
                    }
                }
            }

            PotentialVariableOccurrences = unusedOccurrences;
            ParentState?.OnPotentialVariableIncremented(ref context, variableName);
        }
    }
}
