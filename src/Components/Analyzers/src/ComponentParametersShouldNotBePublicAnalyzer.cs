// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComponentParametersShouldNotBePublicAnalyzer : DiagnosticAnalyzer
    {
        public ComponentParametersShouldNotBePublicAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(DiagnosticDescriptors.ComponentParametersShouldNotBePublic);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var declaration = (PropertyDeclarationSyntax)context.Node;

            var parameterAttribute = declaration.AttributeLists
                .SelectMany(list => list.Attributes)
                .Where(attr => semanticModel.GetTypeInfo(attr).Type?.ToDisplayString() == ComponentsApi.ParameterAttribute.FullTypeName)
                .FirstOrDefault();

            if (parameterAttribute != null && IsPubliclySettable(declaration))
            {
                var identifierText = declaration.Identifier.Text;
                if (!string.IsNullOrEmpty(identifierText))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ComponentParametersShouldNotBePublic,
                        declaration.GetLocation(),
                        identifierText));
                }
            }
        }

        private static bool IsPubliclySettable(PropertyDeclarationSyntax declaration)
        {
            // If the property has a setter explicitly marked private/protected/internal, then it's not public
            var setter = declaration.AccessorList?.Accessors.SingleOrDefault(x => x.Keyword.IsKind(SyntaxKind.SetKeyword));
            if (setter != null && setter.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword) || x.IsKind(SyntaxKind.InternalKeyword)))
            {
                return false;
            }

            // Otherwise fallback to the property declaration modifiers
            return declaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword));
        }
    }
}
