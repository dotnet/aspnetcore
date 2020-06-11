// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.Internal
{
    internal class InternalUsageAnalyzer
    {
        private readonly Func<ISymbol, bool> _isInternalNamespace;
        private readonly Func<ISymbol, bool> _hasInternalAttribute;
        private readonly DiagnosticDescriptor _descriptor;

        /// <summary>
        /// Creates a new instance of <see cref="InternalUsageAnalyzer" />. The creator should provide delegates to help determine whether
        /// a given symbol is internal or not, and a <see cref="DiagnosticDescriptor" /> to create errors.
        /// </summary>
        /// <param name="isInInternalNamespace">The delegate used to check if a symbol belongs to an internal namespace.</param>
        /// <param name="hasInternalAttribute">The delegate used to check if a symbol has an internal attribute.</param>
        /// <param name="descriptor">
        /// The <see cref="DiagnosticDescriptor" /> used to create errors. The error message should expect a single parameter
        /// used for the display name of the member.
        /// </param>
        public InternalUsageAnalyzer(Func<ISymbol, bool> isInInternalNamespace, Func<ISymbol, bool> hasInternalAttribute, DiagnosticDescriptor descriptor)
        {
            _isInternalNamespace = isInInternalNamespace ?? new Func<ISymbol, bool>((_) => false);
            _hasInternalAttribute = hasInternalAttribute ?? new Func<ISymbol, bool>((_) => false);
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public void Register(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.Parameter);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node)
            {
                case MemberAccessExpressionSyntax memberAccessSyntax:
                    {
                        if (context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol is ISymbol symbol &&
                            symbol.ContainingAssembly != context.Compilation.Assembly)
                        {
                            var containingType = symbol.ContainingType;

                            if (HasInternalAttribute(symbol))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, memberAccessSyntax.Name.GetLocation(), $"{containingType}.{symbol.Name}"));
                                return;
                            }

                            if (IsInInternalNamespace(containingType) || HasInternalAttribute(containingType))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, memberAccessSyntax.Name.GetLocation(), containingType));
                                return;
                            }
                        }
                        return;
                    }

                case ObjectCreationExpressionSyntax creationSyntax:
                    {
                        if (context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol is ISymbol symbol &&
                            symbol.ContainingAssembly != context.Compilation.Assembly)
                        {
                            var containingType = symbol.ContainingType;

                            if (HasInternalAttribute(symbol))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, creationSyntax.GetLocation(), containingType));
                                return;
                            }

                            if (IsInInternalNamespace(containingType) || HasInternalAttribute(containingType))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, creationSyntax.Type.GetLocation(), containingType));
                                return;
                            }
                        }

                        return;
                    }

                case ClassDeclarationSyntax declarationSyntax:
                    {
                        if (context.SemanticModel.GetDeclaredSymbol(declarationSyntax)?.BaseType is ISymbol symbol &&
                            symbol.ContainingAssembly != context.Compilation.Assembly &&
                            (IsInInternalNamespace(symbol) || HasInternalAttribute(symbol)) &&
                            declarationSyntax.BaseList?.Types.Count > 0)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(_descriptor, declarationSyntax.BaseList.Types[0].GetLocation(), symbol));
                        }

                        return;
                    }

                case ParameterSyntax parameterSyntax:
                    {
                        if (context.SemanticModel.GetDeclaredSymbol(parameterSyntax)?.Type is ISymbol symbol &&
                            symbol.ContainingAssembly != context.Compilation.Assembly &&
                            (IsInInternalNamespace(symbol) || HasInternalAttribute(symbol)))
                        {

                            context.ReportDiagnostic(Diagnostic.Create(_descriptor, parameterSyntax.GetLocation(), symbol));
                        }

                        return;
                    }
            }
        }

        private bool HasInternalAttribute(ISymbol symbol) => _hasInternalAttribute(symbol);

        private bool IsInInternalNamespace(ISymbol symbol) => _isInternalNamespace(symbol);
    }
}
