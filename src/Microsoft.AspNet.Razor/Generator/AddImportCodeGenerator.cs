// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
