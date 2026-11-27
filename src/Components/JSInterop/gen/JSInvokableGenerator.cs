// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.JSInterop.SourceGenerators;

/// <summary>
/// Experimental generator that registers strongly typed invokers for [JSInvokable] methods,
/// used by the opt-in <c>Microsoft.JSInterop.GeneratedOnly</c> dispatch mode.
/// </summary>
[Generator]
public sealed class JSInvokableGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var parseResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.JSInterop.JSInvokableAttribute",
            predicate: static (node, _) => node is MethodDeclarationSyntax,
            transform: static (ctx, ct) => JSInvokableParser.Parse(ctx, ct));

        context.RegisterSourceOutput(parseResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        var target = context.CompilationProvider.Select(static (compilation, _) =>
        {
            var prototype = compilation.GetTypeByMetadataName("Microsoft.JSInterop.GeneratedInteropPrototype");
            var supported = prototype is not null
                && compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is not null
                && prototype.GetMembers("Shared").Any(m => m.DeclaredAccessibility == Accessibility.Public);
            return (compilation.AssemblyName ?? string.Empty, supported);
        });

        var methods = parseResults.SelectMany(static (result, _) => result.Methods).Collect();

        context.RegisterSourceOutput(methods.Combine(target), static (spc, input) =>
        {
            var (allMethods, (assemblyName, supported)) = input;
            if (!supported || allMethods.IsEmpty)
            {
                return;
            }

            var valid = RemoveDuplicates(spc, allMethods);
            if (!valid.IsEmpty)
            {
                spc.AddSource(JSInvokableEmitter.HintName, JSInvokableEmitter.Emit(assemblyName, valid));
            }
        });
    }

    private static ImmutableArray<JSInvokableMethod> RemoveDuplicates(SourceProductionContext context, ImmutableArray<JSInvokableMethod> methods)
    {
        // Static identifiers are scoped to the assembly and instance identifiers to the declaring type.
        // Registering none of the duplicates keeps module initialization from throwing.
        var duplicates = new HashSet<(bool, string, string)>(
            methods
                .GroupBy(m => (m.IsStatic, m.IsStatic ? string.Empty : m.ContainingType, m.Identifier))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key));

        var builder = ImmutableArray.CreateBuilder<JSInvokableMethod>();
        foreach (var method in methods)
        {
            if (duplicates.Contains((method.IsStatic, method.IsStatic ? string.Empty : method.ContainingType, method.Identifier)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateIdentifier,
                    method.Location?.ToLocation() ?? Location.None,
                    method.Identifier));
            }
            else
            {
                builder.Add(method);
            }
        }

        return builder.ToImmutable();
    }
}
