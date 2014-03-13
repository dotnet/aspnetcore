// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TypeMemberCodeGenerator : SpanCodeGenerator
    {
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddTypeMemberChunk(target.Content, target);
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
