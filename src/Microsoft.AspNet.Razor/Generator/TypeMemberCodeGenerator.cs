// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TypeMemberCodeGenerator : SpanCodeGenerator
    {
        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddTypeMemberChunk(target.Content, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteSnippet(target.Content);
            });

            int paddingCharCount;
            string paddedCode = CodeGeneratorPaddingHelper.Pad(context.Host, generatedCode, target, out paddingCharCount);

            Contract.Assert(paddingCharCount > 0);

            context.GeneratedClass.Members.Add(
                new CodeSnippetTypeMember(paddedCode)
                {
                    LinePragma = context.GenerateLinePragma(target, paddingCharCount)
                });
#endif
            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "TypeMember";
        }

        public override bool Equals(object obj)
        {
            return obj is TypeMemberCodeGenerator;
        }

        // C# complains at us if we don't provide an implementation, even one like this
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
