// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.AddPackage;

namespace Microsoft.AspNetCore.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPackageFixer)), Shared]
public sealed class AddPackageFixer : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        if (semanticModel == null) { return; }

        var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);

        Dictionary<ThisAndExtensionMethod, PackageSourceAndNamespace> _wellKnownExtensionMethodCache = new()
        {
            {
                new(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Authentication_AuthenticationBuilder), "AddJwtBearer"),
                new("Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.Extensions.DependencyInjection")
            },
            {
                new(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Authentication_AuthenticationBuilder), "AddGoogle"),
                new("Microsoft.AspNetCore.Authentication.Google", "Microsoft.Extensions.DependencyInjection")
            }
        };

        foreach (var diagnostic in context.Diagnostics)
        {
            var location = diagnostic.Location.SourceSpan;
            var node = root.FindNode(location);
            if (node == null)
            {
                return;
            }
            string? methodName = node is IdentifierNameSyntax identifier ? identifier.Identifier.Text : null;
            if (methodName == null)
            {
                return;
            }
            var symbol = semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)node.Parent!).Expression).Symbol;
            var symbolName = ((IMethodSymbol)symbol!).ReturnType;
            var targetThisAndExtensionMethod = new ThisAndExtensionMethod(symbolName, methodName);
            if (_wellKnownExtensionMethodCache.TryGetValue(targetThisAndExtensionMethod, out var packageSourceAndNamespace))
            {
                var position = diagnostic.Location.SourceSpan.Start;
                var codeAction = await AspNetCoreAddPackageCodeAction.TryCreateCodeActionAsync(
                    context.Document,
                    position,
                    new AspNetCoreInstallPackageData(null!,
                        packageSourceAndNamespace.packageName,
                        null!,
                        packageSourceAndNamespace.namespaceName),
                    context.CancellationToken);
                if (codeAction != null)
                {
                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }

        }

    }

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS1061");
}

internal struct ThisAndExtensionMethod(ITypeSymbol thisType, string extensionMethod)
{
    public ITypeSymbol ThisType { get; } = thisType;
    public string ExtensionMethod { get; } = extensionMethod;
    public override bool Equals(object? obj)
    {
        if (obj is ThisAndExtensionMethod other)
        {
            return SymbolEqualityComparer.Default.Equals(ThisType, other.ThisType) &&
                ExtensionMethod.Equals(other.ExtensionMethod, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode() => 2;
}
internal record struct PackageSourceAndNamespace(string packageName, string namespaceName);
