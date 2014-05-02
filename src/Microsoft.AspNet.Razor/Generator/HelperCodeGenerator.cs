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

using System.Globalization;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class HelperCodeGenerator : BlockCodeGenerator
    {
        public HelperCodeGenerator(LocationTagged<string> signature, bool headerComplete)
        {
            Signature = signature;
            HeaderComplete = headerComplete;
        }

        public LocationTagged<string> Signature { get; private set; }
        public LocationTagged<string> Footer { get; set; }
        public bool HeaderComplete { get; private set; }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            var chunk = context.CodeTreeBuilder.StartChunkBlock<HelperChunk>(target, topLevel: true);

            chunk.Signature = Signature;
            chunk.Footer = Footer;
            chunk.HeaderComplete = HeaderComplete;
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override bool Equals(object obj)
        {
            HelperCodeGenerator other = obj as HelperCodeGenerator;
            return other != null &&
                   base.Equals(other) &&
                   HeaderComplete == other.HeaderComplete &&
                   Equals(Signature, other.Signature);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(Signature)
                .CombinedHash;
        }

        public override string ToString()
        {
            return "Helper:" + Signature.ToString("F", CultureInfo.CurrentCulture) + ";" + (HeaderComplete ? "C" : "I");
        }
    }
}
