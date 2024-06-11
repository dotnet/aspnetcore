// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.AddPackage;

namespace Microsoft.AspNetCore.Analyzers.Dependencies;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPackageFixer)), Shared]
public class AddPackageFixer : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);
        var wellKnownExtensionMethodCache = ExtensionMethodsClass.ConstructFromWellKnownTypes(wellKnownTypes);

        foreach (var diagnostic in context.Diagnostics)
        {
            var location = diagnostic.Location.SourceSpan;
            var node = root.FindNode(location);
            if (node == null)
            {
                return;
            }
            var methodName = node is IdentifierNameSyntax identifier ? identifier.Identifier.Text : null;
            if (methodName == null)
            {
                return;
            }

            if (node.Parent is not MemberAccessExpressionSyntax)
            {
                return;
            }

            var symbol = semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)node.Parent).Expression).Symbol;
            var symbolType = symbol switch
            {
                IMethodSymbol methodSymbol => methodSymbol.ReturnType,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                ILocalSymbol localSymbol => localSymbol.Type,
                _ => null
            };

            if (symbolType == null)
            {
                return;
            }

            var targetThisAndExtensionMethod = new ThisAndExtensionMethod(symbolType, methodName);
            if (wellKnownExtensionMethodCache.TryGetValue(targetThisAndExtensionMethod, out var packageSourceAndNamespace))
            {
                var position = diagnostic.Location.SourceSpan.Start;
                var packageInstallData = new AspNetCoreInstallPackageData(
                    packageSource: null,
                    packageName: packageSourceAndNamespace.packageName,
                    packageVersionOpt: null,
                    packageNamespaceName: packageSourceAndNamespace.namespaceName);
                var codeAction = await TryCreateCodeActionAsync(
                    context.Document,
                    position,
                    packageInstallData,
                    context.CancellationToken);

                if (codeAction != null)
                {
                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }

        }
    }

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CS1061"];

    internal virtual async Task<CodeAction?> TryCreateCodeActionAsync(
        Document document,
        int position,
        AspNetCoreInstallPackageData packageInstallData,
        CancellationToken cancellationToken)
    {
        var codeAction = await AspNetCoreAddPackageCodeAction.TryCreateCodeActionAsync(
            document,
            position,
            packageInstallData,
            cancellationToken);

        return codeAction;
    }
}
