// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class AddImportCodeGenerator : SpanCodeGenerator
    {
        public AddImportCodeGenerator(string ns, int namespaceKeywordLength)
        {
            Namespace = ns;
            NamespaceKeywordLength = namespaceKeywordLength;
        }

        public string Namespace { get; private set; }
        public int NamespaceKeywordLength { get; set; }

        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            string ns = Namespace;

            if (!String.IsNullOrEmpty(ns) && Char.IsWhiteSpace(ns[0]))
            {
                ns = ns.Substring(1);
            }

            codeTreeBuilder.AddUsingChunk(ns, target, context);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM in CoreCLR.
            // #if'd the entire section because once we transition over to the CodeTree we will not need all this code.

            // Try to find the namespace in the existing imports
            string ns = Namespace;
            if (!String.IsNullOrEmpty(ns) && Char.IsWhiteSpace(ns[0]))
            {
                ns = ns.Substring(1);
            }

            CodeNamespaceImport import = context.Namespace
                .Imports
                .OfType<CodeNamespaceImport>()
                .Where(i => String.Equals(i.Namespace, ns.Trim(), StringComparison.Ordinal))
                .FirstOrDefault();

            if (import == null)
            {
                // It doesn't exist, create it
                import = new CodeNamespaceImport(ns);
                context.Namespace.Imports.Add(import);
            }

            // Attach our info to the existing/new import.
            import.LinePragma = context.GenerateLinePragma(target);
#endif
            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "Import:" + Namespace + ";KwdLen:" + NamespaceKeywordLength;
        }

        public override bool Equals(object obj)
        {
            AddImportCodeGenerator other = obj as AddImportCodeGenerator;
            return other != null &&
                   String.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
                   NamespaceKeywordLength == other.NamespaceKeywordLength;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Namespace)
                .Add(NamespaceKeywordLength)
                .CombinedHash;
        }
    }
}
