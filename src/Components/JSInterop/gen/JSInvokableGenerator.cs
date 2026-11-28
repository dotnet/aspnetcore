// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.JSInterop.SourceGenerators;

/// <summary>
/// Experimental generator that registers strongly typed invokers for [JSInvokable] methods,
/// used by the opt-in <c>Microsoft.JSInterop.GeneratedOnly</c> dispatch mode.
/// </summary>
[Generator]
public sealed class JSInvokableGenerator : IIncrementalGenerator
{
    internal const string EnabledProperty = "build_property.JSInteropGenerateInvokers";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generated-only dispatch is selected at build time. Without the property the generator emits
        // no registrations and no diagnostics; MSBuild also detaches it so the scan does not run.
        var enabled = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            options.GlobalOptions.TryGetValue(EnabledProperty, out var value)
            && string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase));

        var parseResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.JSInterop.JSInvokableAttribute",
            predicate: static (node, _) => node is MethodDeclarationSyntax,
            transform: static (ctx, ct) => JSInvokableParser.Parse(ctx, ct));

        // Overrides that do not redeclare an inherited identifier hide it from reflection dispatch.
        var overrideResults = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is MethodDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.OverrideKeyword),
            transform: static (ctx, ct) => JSInvokableParser.ParseOverride(ctx, ct));

        var results = parseResults.Collect().Combine(overrideResults.Collect())
            .Select(static (input, _) => input.Left.AddRange(input.Right));

        context.RegisterSourceOutput(results.Combine(enabled), static (spc, input) =>
        {
            var (all, isEnabled) = input;
            if (!isEnabled)
            {
                return;
            }

            foreach (var result in all)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            }
        });

        var target = context.CompilationProvider.Select(static (compilation, _) =>
        {
            var interop = compilation.GetTypeByMetadataName("Microsoft.JSInterop.Infrastructure.GeneratedJSInterop");
            var supported = interop is not null
                && compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is not null
                && compilation.GetTypeByMetadataName("Microsoft.JSInterop.Infrastructure.GeneratedJSInterop+IInstanceRegistrationProvider") is not null
                && interop.GetMembers("Shared").Any(m => m.DeclaredAccessibility == Accessibility.Public)
                && interop.GetMembers("RegisterNonInvokableInstance").Any(m => m.DeclaredAccessibility == Accessibility.Public);
            return (compilation.AssemblyName ?? string.Empty, supported);
        });

        var methods = results.Select(static (all, _) => new EquatableArray<JSInvokableMethod>([.. all.SelectMany(r => r.Methods)]));

        context.RegisterSourceOutput(methods.Combine(target).Combine(enabled), static (spc, input) =>
        {
            var ((allMethods, (assemblyName, supported)), isEnabled) = input;
            if (!isEnabled || !supported || allMethods.Length == 0)
            {
                return;
            }

            var valid = RemoveDuplicates(spc, allMethods.Items);
            var registrations = valid.Where(m => m.Host is null).ToImmutableArray();
            if (!registrations.IsEmpty)
            {
                spc.AddSource(JSInvokableEmitter.HintName, JSInvokableEmitter.Emit(assemblyName, registrations));
            }

            foreach (var hosted in valid.Where(m => m.Host is not null).GroupBy(m => m.Host!))
            {
                var baseHost = hosted.Key.BaseHosts.Items.FirstOrDefault(baseType =>
                    baseType.HasCompiledBridge || valid.Any(m => m.Host is not null && m.ContainingType == baseType.DefinitionName));
                spc.AddSource(hosted.Key.HintName, JSInvokableEmitter.EmitHost(hosted.Key, [.. hosted], baseHost?.TypeName));
            }
        });
    }

    private static ImmutableArray<JSInvokableMethod> RemoveDuplicates(SourceProductionContext context, ImmutableArray<JSInvokableMethod> methods)
    {
        // Static identifiers are scoped to the assembly and instance identifiers to the declaring type.
        // Registering none of the duplicates keeps module initialization from throwing.
        var invokable = methods.Where(m => !m.IsHidden).ToImmutableArray();
        var duplicates = new HashSet<(bool, string, string)>(
            invokable
                .GroupBy(Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key));

        var builder = ImmutableArray.CreateBuilder<JSInvokableMethod>();
        foreach (var method in invokable)
        {
            if (duplicates.Contains(Key(method)))
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

        // A hidden identifier is only recorded when the type does not invoke it through another method;
        // several overrides can hide the same identifier.
        var registered = new HashSet<(bool, string, string)>(invokable.Select(Key));
        foreach (var hidden in methods.Where(m => m.IsHidden))
        {
            if (registered.Add(Key(hidden)))
            {
                builder.Add(hidden);
            }
        }

        return builder.ToImmutable();

        static (bool, string, string) Key(JSInvokableMethod m)
            => (m.IsStatic, m.IsStatic ? string.Empty : m.ContainingType, m.Identifier);
    }
}
