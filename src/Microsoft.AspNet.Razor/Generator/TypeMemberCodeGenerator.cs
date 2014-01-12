// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TypeMemberCodeGenerator : SpanCodeGenerator
    {
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
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
