// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
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

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            string ns = Namespace;

            if (!String.IsNullOrEmpty(ns) && Char.IsWhiteSpace(ns[0]))
            {
                ns = ns.Substring(1);
            }

            context.CodeTreeBuilder.AddUsingChunk(ns, target);
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
