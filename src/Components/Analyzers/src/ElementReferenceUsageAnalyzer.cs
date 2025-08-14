// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ElementReferenceUsageAnalyzer : DiagnosticAnalyzer
{
    public ElementReferenceUsageAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync,
            DiagnosticDescriptors.ElementReferenceUsageInMethodCalledOutsideOnAfterRender);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        
        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!ComponentSymbols.TryCreate(compilationContext.Compilation, out var symbols))
            {
                // Types we need are not defined.
                return;
            }

            if (symbols.ElementReferenceType == null)
            {
                // ElementReference type not available.
                return;
            }

            // Register analysis for component types only
            compilationContext.RegisterSymbolStartAction(symbolStartContext =>
            {
                var namedTypeSymbol = (INamedTypeSymbol)symbolStartContext.Symbol;
                
                // Only analyze types that implement IComponent
                if (!IsComponentType(namedTypeSymbol, symbols))
                {
                    return;
                }

                var analyzer = new ComponentAnalyzer(symbols);
                symbolStartContext.RegisterOperationAction(analyzer.AnalyzeOperation, 
                    OperationKind.FieldReference, 
                    OperationKind.PropertyReference,
                    OperationKind.Invocation);
                symbolStartContext.RegisterSymbolEndAction(analyzer.AnalyzeSymbolEnd);
                
            }, SymbolKind.NamedType);
        });
    }

    private static bool IsComponentType(INamedTypeSymbol namedTypeSymbol, ComponentSymbols symbols)
    {
        if (symbols.IComponentType == null)
        {
            return false;
        }

        return namedTypeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, symbols.IComponentType));
    }

    private class ComponentAnalyzer
    {
        private readonly ComponentSymbols _symbols;
        private readonly HashSet<IMethodSymbol> _methodsWithElementReferenceAccess = new();
        private readonly HashSet<IMethodSymbol> _methodsCalledFromOnAfterRender = new();
        private readonly Dictionary<IMethodSymbol, List<(IOperation operation, ISymbol elementRef)>> _elementReferenceAccesses = new();
        private readonly Dictionary<IMethodSymbol, List<(IOperation operation, IMethodSymbol calledMethod)>> _methodInvocations = new();

        public ComponentAnalyzer(ComponentSymbols symbols)
        {
            _symbols = symbols;
        }

        public void AnalyzeOperation(OperationAnalysisContext context)
        {
            var containingMethod = GetContainingMethod(context.Operation);
            if (containingMethod == null)
            {
                return;
            }

            switch (context.Operation)
            {
                case IMemberReferenceOperation memberRef:
                    AnalyzeMemberReference(memberRef, containingMethod);
                    break;
                case IInvocationOperation invocation:
                    AnalyzeMethodInvocation(invocation, containingMethod);
                    break;
            }
        }

        private void AnalyzeMemberReference(IMemberReferenceOperation memberRef, IMethodSymbol containingMethod)
        {
            var memberSymbol = memberRef.Member;
            var memberType = GetMemberType(memberSymbol);
            
            // Check if the member is of ElementReference type
            if (memberType == null || !IsElementReferenceType(memberType))
            {
                return;
            }

            // Skip if this is part of AddElementReferenceCapture call (BuildRenderTree)
            if (IsElementReferenceCaptureCall(memberRef))
            {
                return;
            }

            // Track this method as having ElementReference access
            _methodsWithElementReferenceAccess.Add(containingMethod);
            
            if (!_elementReferenceAccesses.TryGetValue(containingMethod, out var accesses))
            {
                accesses = new List<(IOperation, ISymbol)>();
                _elementReferenceAccesses[containingMethod] = accesses;
            }
            accesses.Add((memberRef, memberSymbol));
        }

        private void AnalyzeMethodInvocation(IInvocationOperation invocation, IMethodSymbol containingMethod)
        {
            var targetMethod = invocation.TargetMethod;
            
            // Only track method calls within the same type
            if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, containingMethod.ContainingType))
            {
                return;
            }

            if (!_methodInvocations.TryGetValue(containingMethod, out var invocations))
            {
                invocations = new List<(IOperation, IMethodSymbol)>();
                _methodInvocations[containingMethod] = invocations;
            }
            invocations.Add((invocation, targetMethod));
        }

        public void AnalyzeSymbolEnd(SymbolAnalysisContext context)
        {
            // First, identify methods called from OnAfterRender/OnAfterRenderAsync
            PropagateOnAfterRenderContext();

            // Now report diagnostics
            foreach (var methodWithElementRef in _methodsWithElementReferenceAccess)
            {
                // Skip OnAfterRender methods themselves - they are always safe
                if (IsOnAfterRenderMethod(methodWithElementRef))
                {
                    continue;
                }

                // Check if this method is called from anywhere outside OnAfterRender
                var isCalledFromOutsideOnAfterRender = IsMethodCalledFromOutsideOnAfterRender(methodWithElementRef);

                if (isCalledFromOutsideOnAfterRender)
                {
                    // Report diagnostic for helper method called outside OnAfterRender
                    if (_elementReferenceAccesses.TryGetValue(methodWithElementRef, out var accesses))
                    {
                        foreach (var (operation, elementRef) in accesses)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ElementReferenceUsageInMethodCalledOutsideOnAfterRender,
                                operation.Syntax.GetLocation(),
                                methodWithElementRef.Name,
                                elementRef.Name));
                        }
                    }
                }
                else
                {
                    // This method is not called from outside OnAfterRender contexts.
                    // Check if it's actually called from anywhere, or if it's a standalone method.
                    var isCalledFromAnywhere = _methodInvocations.Values.Any(invocations => 
                        invocations.Any(call => SymbolEqualityComparer.Default.Equals(call.calledMethod, methodWithElementRef)));

                    if (!isCalledFromAnywhere)
                    {
                        // Method is not called from anywhere, so report standard diagnostic for direct access
                        if (_elementReferenceAccesses.TryGetValue(methodWithElementRef, out var accesses))
                        {
                            foreach (var (operation, elementRef) in accesses)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync,
                                    operation.Syntax.GetLocation(),
                                    elementRef.Name));
                            }
                        }
                    }
                    // If the method is called from somewhere but not from outside OnAfterRender, then it's safe
                }
            }
        }

        private void PropagateOnAfterRenderContext()
        {
            var visited = new HashSet<IMethodSymbol>();
            var toVisit = new Queue<IMethodSymbol>();

            // Start with OnAfterRender methods
            foreach (var method in _methodInvocations.Keys.Concat(_methodsWithElementReferenceAccess))
            {
                if (IsOnAfterRenderMethod(method))
                {
                    toVisit.Enqueue(method);
                    _methodsCalledFromOnAfterRender.Add(method);
                }
            }

            // Propagate to methods called from OnAfterRender
            while (toVisit.Count > 0)
            {
                var currentMethod = toVisit.Dequeue();
                if (visited.Contains(currentMethod))
                {
                    continue;
                }
                visited.Add(currentMethod);

                if (_methodInvocations.TryGetValue(currentMethod, out var invocations))
                {
                    foreach (var (_, calledMethod) in invocations)
                    {
                        if (!_methodsCalledFromOnAfterRender.Contains(calledMethod))
                        {
                            _methodsCalledFromOnAfterRender.Add(calledMethod);
                            toVisit.Enqueue(calledMethod);
                        }
                    }
                }
            }
        }

        private bool IsMethodCalledFromOutsideOnAfterRender(IMethodSymbol method)
        {
            // Check if the method is called from any method that is not in the OnAfterRender context
            foreach (var invocationKvp in _methodInvocations)
            {
                var callingMethod = invocationKvp.Key;
                var calledMethods = invocationKvp.Value;

                if (calledMethods.Any(call => SymbolEqualityComparer.Default.Equals(call.calledMethod, method)))
                {
                    // This method is called by callingMethod
                    if (!IsOnAfterRenderMethod(callingMethod) && !_methodsCalledFromOnAfterRender.Contains(callingMethod))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsElementReferenceCaptureCall(IMemberReferenceOperation memberRef)
        {
            // Check if this member reference is part of an AddElementReferenceCapture call
            var parent = memberRef.Parent;
            while (parent != null)
            {
                if (parent is IInvocationOperation invocation)
                {
                    var methodName = invocation.TargetMethod.Name;
                    if (methodName == "AddElementReferenceCapture")
                    {
                        return true;
                    }
                }
                parent = parent.Parent;
            }
            return false;
        }

        private bool IsElementReferenceType(ITypeSymbol type)
        {
            return SymbolEqualityComparer.Default.Equals(type, _symbols.ElementReferenceType);
        }

        private static ITypeSymbol GetMemberType(ISymbol memberSymbol)
        {
            return memberSymbol switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                _ => null
            };
        }

        private static IMethodSymbol GetContainingMethod(IOperation operation)
        {
            var current = operation;
            while (current != null)
            {
                if (current.SemanticModel != null)
                {
                    var symbol = current.SemanticModel.GetEnclosingSymbol(current.Syntax.SpanStart);
                    if (symbol is IMethodSymbol methodSymbol)
                    {
                        return methodSymbol;
                    }
                }
                current = current.Parent;
            }
            return null;
        }

        private static bool IsOnAfterRenderMethod(IMethodSymbol methodSymbol)
        {
            return methodSymbol.Name == "OnAfterRenderAsync" || methodSymbol.Name == "OnAfterRender";
        }
    }
}