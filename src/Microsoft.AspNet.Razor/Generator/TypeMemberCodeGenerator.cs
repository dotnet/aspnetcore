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
