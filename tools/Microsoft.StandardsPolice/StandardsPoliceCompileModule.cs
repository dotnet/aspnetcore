using Microsoft.Dnx.Compilation.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.StandardsPolice
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class StandardsPoliceCompileModule : ICompileModule
    {
        public void BeforeCompile(BeforeCompileContext context)
        {
            ScanNamespace(context.Diagnostics, context.Compilation.GlobalNamespace);

            foreach (var st in context.Compilation.SyntaxTrees)
            {
                if (!st.FilePath.EndsWith(".Generated.cs"))
                {
                    ScanSyntaxTree(context.Diagnostics, st);
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
            foreach (var member in typeSymbol.GetTypeMembers())
            {
                ScanType(diagnostics, member);
            }
        }
        public void AfterCompile(AfterCompileContext context)
        {
        }
    }
}
