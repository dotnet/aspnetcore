// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;
using System;

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

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
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
