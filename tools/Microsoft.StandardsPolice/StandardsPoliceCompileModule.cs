using Microsoft.Dnx.Compilation.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Microsoft.StandardsPolice
{
    public class StandardsPoliceCompileModule : ICompileModule
    {
        public void BeforeCompile(BeforeCompileContext context)
        {
            ScanCompilation(context.Diagnostics, context.Compilation);
        }

        internal static void ScanCompilation(IList<Diagnostic> diagnostics, CSharpCompilation compilation)
        {
            ScanNamespace(diagnostics, compilation.GlobalNamespace);

            foreach (var st in compilation.SyntaxTrees)
            {
                if (!st.FilePath.EndsWith(".Generated.cs"))
                {
                    ScanSyntaxTree(diagnostics, st);
                }
            }
        }

        internal static void ScanSyntaxTree(IList<Diagnostic> diagnostics, SyntaxTree syntaxTree)
        {
            var root = syntaxTree.GetRoot();

            var typeDeclarations = root.DescendantNodes(descendIntoChildren: node => !(node is TypeDeclarationSyntax))
                .OfType<TypeDeclarationSyntax>()
                .ToArray();

            if (typeDeclarations.Length > 1)
            {
                foreach (var typeDeclaration in typeDeclarations)
                {
                    diagnostics.Add(Diagnostic.Create(
                        "SP1002", "StandardsPolice", "more than one type per file",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        false,
                        3,
                        location: typeDeclaration.GetLocation()));
                }
            }

            var usingDirectives = root.DescendantNodes(descendIntoChildren: node => !(node is TypeDeclarationSyntax))
                .OfType<UsingDirectiveSyntax>()
                .ToArray();

            var priorUsingDirective = default(UsingDirectiveSyntax);
            foreach (var usingDirective in usingDirectives)
            {
                var acceptableOrder = false;
                if (!acceptableOrder && priorUsingDirective == null)
                {
                    acceptableOrder = true;
                }
                if (!acceptableOrder && string.Compare(priorUsingDirective.Name.ToString(), usingDirective.Name.ToString(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    acceptableOrder = true;
                }
                if (!acceptableOrder &&
                    priorUsingDirective.Name.ToString().StartsWith("System.") &&
                    !usingDirective.Name.ToString().StartsWith("System."))
                {
                    acceptableOrder = true;
                }
                if (!acceptableOrder)
                {
                    diagnostics.Add(Diagnostic.Create(
                        "SP1004", "StandardsPolice", "namespaces not alphabetized",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        false,
                        3,
                        location: usingDirective.GetLocation()));
                }
                priorUsingDirective = usingDirective;
            }
        }

        private static void ScanNamespace(IList<Diagnostic> diagnostics, INamespaceSymbol namespaceSymbol)
        {
            foreach (var member in namespaceSymbol.GetNamespaceMembers())
            {
                ScanNamespace(diagnostics, member);
            }
            foreach (var member in namespaceSymbol.GetTypeMembers())
            {
                ScanType(diagnostics, member);
            }
        }

        private static void ScanType(IList<Diagnostic> diagnostics, INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.Locations.Any(location => location.IsInSource))
            {
                RuleFieldPrivateKeyword(diagnostics, typeSymbol);
                RuleMembersAreInCorrectOrder(diagnostics, typeSymbol, MapClassMembers);
            }

            foreach (var member in typeSymbol.GetTypeMembers())
            {
                ScanType(diagnostics, member);
            }
        }


        private static void RuleFieldPrivateKeyword(IList<Diagnostic> diagnostics, INamedTypeSymbol typeSymbol)
        {
            foreach (var member in typeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.DeclaredAccessibility != Accessibility.Private)
                {
                    continue;
                }

                foreach (var syntaxReference in member.DeclaringSyntaxReferences)
                {
                    var fieldHasPrivateKeyword = false;
                    var syntax = syntaxReference.GetSyntax();
                    var fds = syntax?.Parent?.Parent as FieldDeclarationSyntax;
                    if (fds == null)
                    {
                        continue;
                    }
                    foreach (var mod in fds.Modifiers)
                    {
                        if (mod.IsKind(CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword))
                        {
                            fieldHasPrivateKeyword = true;
                        }
                    }
                    if (!fieldHasPrivateKeyword)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            "SP1001", "StandardsPolice", "private keyword missing",
                            DiagnosticSeverity.Warning,
                            DiagnosticSeverity.Warning,
                            false,
                            3,
                            location: member.Locations.SingleOrDefault()));
                    }
                }
            }
        }

        private static void RuleMembersAreInCorrectOrder(IList<Diagnostic> diagnostics, INamedTypeSymbol typeSymbol, Func<ISymbol, ClassZone> mapZone)
        {
            if (typeSymbol.Locations.Length >= 2 || typeSymbol.Name == "Libuv")
            {
                // Don't apply to partial classes. All members are enumerated, but are not merged by zone order.
                return;
            }

            var currentZone = ClassZone.BeforeStart;
            var currentZoneExample = default(ISymbol);
            foreach (var member in typeSymbol.GetMembers())
            {
                var memberZone = mapZone(member);
                if (memberZone == ClassZone.Ignored)
                {
                    continue;
                }
                if (currentZone <= memberZone)
                {
                    currentZone = memberZone;
                    currentZoneExample = member;
                }
                if (memberZone >= ClassZone.OtherThings)
                {
                    continue;
                }
                if (memberZone < currentZone)
                {
                    if (member.Locations.Count() == 1)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            "SP1003", "StandardsPolice", $"{memberZone} like {typeSymbol.Name}::{member.Name} shouldn't be after {currentZone} like {currentZoneExample.Name}",
                            DiagnosticSeverity.Warning,
                            DiagnosticSeverity.Warning,
                            false,
                            3,
                            location: member.Locations.Single(),
                            additionalLocations: currentZoneExample.Locations));
                    }
                }
            }
            currentZone = ClassZone.AfterEnd;
            currentZoneExample = null;
            foreach (var member in typeSymbol.GetMembers().Reverse())
            {
                var memberZone = mapZone(member);
                if (memberZone == ClassZone.Ignored)
                {
                    continue;
                }
                if (currentZone >= memberZone)
                {
                    currentZone = memberZone;
                    currentZoneExample = member;
                }
                if (memberZone <= ClassZone.OtherThings)
                {
                    continue;
                }
                if (memberZone > currentZone)
                {
                    if (member.Locations.Count() == 1)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            "SP1003", "StandardsPolice", $"{memberZone} like {typeSymbol.Name}::{member.Name} shouldn't be before {currentZone} like {currentZoneExample.Name}",
                            DiagnosticSeverity.Warning,
                            DiagnosticSeverity.Warning,
                            false,
                            3,
                            location: member.Locations.Single(),
                            additionalLocations: currentZoneExample.Locations));
                    }
                }
            }
        }

        private static ClassZone MapClassMembers(ISymbol member)
        {
            if (member.IsImplicitlyDeclared)
            {
                return ClassZone.Ignored;
            }
            if (member.Kind == SymbolKind.Field)
            {
                return ClassZone.Fields;
            }
            if (member.Kind == SymbolKind.Method)
            {
                var method = (IMethodSymbol)member;
                if (method.MethodKind == MethodKind.Constructor ||
                    method.MethodKind == MethodKind.StaticConstructor)
                {
                    return ClassZone.Constructors;
                }
                if (method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet)
                {
                    return ClassZone.Properties;
                }
            }
            if (member.Kind == SymbolKind.Property)
            {
                return ClassZone.Properties;
            }
            if (member.Kind == SymbolKind.NamedType)
            {
                var namedType = (INamedTypeSymbol)member;
                if (namedType.TypeKind == TypeKind.Class ||
                    namedType.TypeKind == TypeKind.Enum ||
                    namedType.TypeKind == TypeKind.Struct)
                {
                    return ClassZone.NestedTypes;
                }
            }
            return ClassZone.OtherThings;
        }

        public void AfterCompile(AfterCompileContext context)
        {
        }

        private enum ClassZone
        {
            Ignored,
            BeforeStart,
            Fields,
            Constructors,
            Properties,
            OtherThings,
            NestedTypes,
            AfterEnd
        }
    }
}
