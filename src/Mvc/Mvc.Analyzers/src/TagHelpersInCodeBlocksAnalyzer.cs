// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TagHelpersInCodeBlocksAnalyzer : DiagnosticAnalyzer
{
    public TagHelpersInCodeBlocksAnalyzer()
    {
        TagHelperInCodeBlockDiagnostic = DiagnosticDescriptors.MVC1006_FunctionsContainingTagHelpersMustBeAsyncAndReturnTask;
        SupportedDiagnostics = ImmutableArray.Create(new[] { TagHelperInCodeBlockDiagnostic });
    }

    private DiagnosticDescriptor TagHelperInCodeBlockDiagnostic { get; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Generated Razor code is considered auto generated. By default analyzers skip over auto-generated code unless we say otherwise.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (!SymbolCache.TryCreate(context.Compilation, out var symbolCache))
            {
                // No-op if we can't find types we care about.
                return;
            }

            InitializeWorker(context, symbolCache);
        });
    }

    private void InitializeWorker(CompilationStartAnalysisContext context, SymbolCache symbolCache)
    {
        context.RegisterOperationBlockStartAction(startBlockContext =>
        {
            var capturedDiagnosticLocations = new HashSet<Location>();
            startBlockContext.RegisterOperationAction(context =>
            {
                var awaitOperation = (IAwaitOperation)context.Operation;

                if (awaitOperation.Operation.Kind != OperationKind.Invocation)
                {
                    return;
                }

                var invocationOperation = (IInvocationOperation)awaitOperation.Operation;

                if (!IsTagHelperRunnerRunAsync(invocationOperation.TargetMethod, symbolCache))
                {
                    return;
                }

                var parent = context.Operation.Parent;
                while (parent != null && !IsParentMethod(parent))
                {
                    parent = parent.Parent;
                }

                if (parent == null)
                {
                    return;
                }

                var methodSymbol = (IMethodSymbol?)(parent switch
                {
                    ILocalFunctionOperation localFunctionOperation => localFunctionOperation.Symbol,
                    IAnonymousFunctionOperation anonymousFunctionOperation => anonymousFunctionOperation.Symbol,
                    IMethodBodyOperation methodBodyOperation => startBlockContext.OwningSymbol,
                    _ => null,
                });

                if (methodSymbol == null)
                {
                    // Unsupported operation type.
                    return;
                }

                if (!methodSymbol.IsAsync ||
                    !symbolCache.TaskType.IsAssignableFrom(methodSymbol.ReturnType))
                {
                    capturedDiagnosticLocations.Add(parent.Syntax.GetLocation());
                }
            }, OperationKind.Await);

            startBlockContext.RegisterOperationBlockEndAction(context =>
            {
                foreach (var location in capturedDiagnosticLocations)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(TagHelperInCodeBlockDiagnostic, location));
                }
            });
        });
    }

    private static bool IsTagHelperRunnerRunAsync(IMethodSymbol method, SymbolCache symbolCache)
    {
        if (!SymbolEqualityComparer.Default.Equals(method, symbolCache.TagHelperRunnerRunAsyncMethodSymbol))
        {
            return false;
        }

        return true;
    }

    private static bool IsParentMethod(IOperation operation)
    {
        if (operation.Kind == OperationKind.LocalFunction)
        {
            return true;
        }

        if (operation.Kind == OperationKind.MethodBody)
        {
            return true;
        }

        if (operation.Kind == OperationKind.AnonymousFunction)
        {
            return true;
        }

        return false;
    }

    private readonly struct SymbolCache
    {
        private SymbolCache(
            IMethodSymbol tagHelperRunnerRunAsyncMethodSymbol,
            INamedTypeSymbol taskType)
        {
            TagHelperRunnerRunAsyncMethodSymbol = tagHelperRunnerRunAsyncMethodSymbol;
            TaskType = taskType;
        }

        public IMethodSymbol TagHelperRunnerRunAsyncMethodSymbol { get; }

        public INamedTypeSymbol TaskType { get; }

        public static bool TryCreate(Compilation compilation, out SymbolCache symbolCache)
        {
            symbolCache = default;

            if (!TryGetType(SymbolNames.TagHelperRunnerTypeName, out var tagHelperRunnerType))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.TaskTypeName, out var taskType))
            {
                return false;
            }

            var members = tagHelperRunnerType.GetMembers(SymbolNames.RunAsyncMethodName);
            if (members.Length == 0)
            {
                return false;
            }

            var tagHelperRunnerRunAsyncMethod = (IMethodSymbol)members[0];

            symbolCache = new SymbolCache(tagHelperRunnerRunAsyncMethod, taskType);
            return true;

            bool TryGetType(string typeName, out INamedTypeSymbol typeSymbol)
            {
                typeSymbol = compilation.GetTypeByMetadataName(typeName);
                return typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error;
            }
        }
    }
}
