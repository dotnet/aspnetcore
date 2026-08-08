// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Components.Endpoints.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

/// <summary>
/// Generates application metadata for JS-invokable methods declared by a Razor components application.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed partial class RazorComponentsMetadataGenerator : IIncrementalGenerator
{
    private static readonly string[] BuiltInJSInvokableDescriptorAssemblies =
    [
        "Microsoft.AspNetCore.Components.Web",
    ];

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateContextDeclaration(node),
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Collect();

        var models = context.CompilationProvider
            .Combine(candidates)
            .Select(static (pair, cancellationToken) => Build(pair.Left, pair.Right, cancellationToken));

        context.RegisterSourceOutput(models, static (spc, result) => Emit(spc, result));
    }

    private static bool IsCandidateContextDeclaration(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList: not null };

    private static GenerationResult Build(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.IsDefaultOrEmpty)
        {
            return GenerationResult.Empty;
        }

        var expanded = compilation.Options is CSharpCompilationOptions options
            ? compilation.WithOptions(options.WithMetadataImportOptions(MetadataImportOptions.All))
            : compilation;

        var types = WellKnownTypes.Create(expanded);
        if (types is null)
        {
            return GenerationResult.Empty;
        }

        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var contexts = ResolveContexts(expanded, candidates, types, diagnostics, cancellationToken);
        if (contexts.Count == 0)
        {
            return new GenerationResult(ImmutableArray<MetadataContextModel>.Empty, diagnostics.ToImmutable());
        }

        var jsInvokableMethods = CollectJSInvokableMethods(expanded, types, cancellationToken);
        var referencedAssemblyNames = new HashSet<string>(
            expanded.SourceModule.ReferencedAssemblySymbols.Select(static assembly => assembly.Identity.Name),
            StringComparer.Ordinal);
        var builtInJSInvokableDescriptorAssemblies = BuiltInJSInvokableDescriptorAssemblies
            .Where(referencedAssemblyNames.Contains)
            .ToImmutableArray();
        var models = ImmutableArray.CreateBuilder<MetadataContextModel>();
        foreach (var contextType in contexts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bindableTypes = CollectBindableTypes(contextType, types, diagnostics, cancellationToken);
            models.Add(new MetadataContextModel(
                Namespace: contextType.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
                ContainingTypes: GetContainingTypeNames(contextType),
                TypeName: contextType.Name,
                TypeKeyword: contextType.IsRecord ? "record" : "class",
                DeclaresJsonTypeInfoResolver: DeclaresMember(contextType, "JsonTypeInfoResolver"),
                BuiltInJSInvokableDescriptorAssemblies: builtInJSInvokableDescriptorAssemblies,
                BindableTypes: bindableTypes,
                JSInvokableMethods: jsInvokableMethods));
        }

        return new GenerationResult(models.ToImmutable(), diagnostics.ToImmutable());
    }

    private static List<INamedTypeSymbol> ResolveContexts(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> candidates,
        WellKnownTypes types,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var results = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tree = compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == candidate.SyntaxTree.FilePath)
                ?? candidate.SyntaxTree;
            if (!compilation.ContainsSyntaxTree(tree))
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(tree);
            var node = ReferenceEquals(tree, candidate.SyntaxTree)
                ? candidate
                : tree.GetRoot(cancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == candidate.Identifier.ValueText &&
                                         c.SpanStart == candidate.SpanStart);
            if (node is null)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is not INamedTypeSymbol symbol ||
                !SymbolHelpers.IsOrInheritsFrom(symbol.BaseType, types.MetadataContext))
            {
                continue;
            }

            var nonPartialType = GetFirstNonPartialType(symbol, cancellationToken);
            if (nonPartialType is not null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.MetadataContextMustBePartial.Id,
                    nonPartialType.FullName(),
                    string.Empty));
                continue;
            }

            if (seen.Add(symbol))
            {
                results.Add(symbol);
            }
        }

        return results;
    }

    private static INamedTypeSymbol? GetFirstNonPartialType(
        INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (!current.DeclaringSyntaxReferences.Any(reference =>
                    reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration &&
                    declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                return current;
            }
        }

        return null;
    }

    private static ImmutableArray<ContainingTypeModel> GetContainingTypeNames(INamedTypeSymbol type)
    {
        var names = new List<ContainingTypeModel>();
        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            names.Insert(0, new ContainingTypeModel(
                container.Name,
                GetTypeKeyword(container),
                GetTypeParameters(container),
                GetConstraintClauses(container)));
        }

        return names.ToImmutableArray();
    }

    private static string GetTypeKeyword(INamedTypeSymbol type)
        => (type.TypeKind, type.IsRecord) switch
        {
            (TypeKind.Class, true) => "record",
            (TypeKind.Struct, true) => "record struct",
            (TypeKind.Class, false) => "class",
            (TypeKind.Struct, false) => "struct",
            (TypeKind.Interface, false) => "interface",
            _ => throw new InvalidOperationException($"Unsupported containing type kind '{type.TypeKind}'."),
        };

    private static ImmutableArray<string> GetTypeParameters(INamedTypeSymbol type)
        => type.TypeParameters.Select(parameter =>
        {
            var variance = parameter.Variance switch
            {
                VarianceKind.In => "in ",
                VarianceKind.Out => "out ",
                _ => string.Empty,
            };
            return variance + parameter.Name;
        }).ToImmutableArray();

    private static ImmutableArray<string> GetConstraintClauses(INamedTypeSymbol type)
    {
        var clauses = ImmutableArray.CreateBuilder<string>();
        foreach (var parameter in type.TypeParameters)
        {
            var constraints = new List<string>();
            if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                    ? "class?"
                    : "class");
            }
            else if (parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(constraint => constraint.AnnotatedFullName()));
            if (parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                clauses.Add($"where {parameter.Name} : {string.Join(", ", constraints)}");
            }
        }

        return clauses.ToImmutable();
    }

    private static bool DeclaresMember(INamedTypeSymbol type, string name)
        => type.GetMembers(name).Length > 0;

    internal sealed record class GenerationResult(
        ImmutableArray<MetadataContextModel> Models,
        ImmutableArray<DiagnosticInfo> Diagnostics)
    {
        public static readonly GenerationResult Empty = new(ImmutableArray<MetadataContextModel>.Empty, ImmutableArray<DiagnosticInfo>.Empty);

        public bool Equals(GenerationResult? other)
            => other is not null &&
               ModelComparer.SequenceEqual(Models, other.Models) &&
               ModelComparer.SequenceEqual(Diagnostics, other.Diagnostics);

        public override int GetHashCode() => ModelComparer.AddRange(0, Models);
    }

    internal sealed record class DiagnosticInfo(string Id, string Argument0, string Argument1)
    {
        public Diagnostic ToDiagnostic()
        {
            var descriptor = Id switch
            {
                "BLAZORAOT002" => DiagnosticDescriptors.BindableModelNotDescribed,
                _ => DiagnosticDescriptors.MetadataContextMustBePartial,
            };

            return Diagnostic.Create(descriptor, Location.None, Argument0, Argument1);
        }
    }
}
