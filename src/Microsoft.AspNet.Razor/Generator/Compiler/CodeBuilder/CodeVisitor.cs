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

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeVisitor<T> : ChunkVisitor<T> where T : CodeWriter
    {
        public CodeVisitor(T writer, CodeGeneratorContext context)
            : base(writer, context) { }

        protected override void Visit(LiteralChunk chunk)
        {
        }
        protected override void Visit(ExpressionBlockChunk chunk)
        {
        }
        protected override void Visit(ExpressionChunk chunk)
        {
        }
        protected override void Visit(StatementChunk chunk)
        {
        }
        protected override void Visit(UsingChunk chunk)
        {
        }
        protected override void Visit(ChunkBlock chunk)
        {
        }
        protected override void Visit(DynamicCodeAttributeChunk chunk)
        {
        }
        protected override void Visit(LiteralCodeAttributeChunk chunk)
        {
        }
        protected override void Visit(CodeAttributeChunk chunk)
        {
        }
        protected override void Visit(HelperChunk chunk)
        {
        }
        protected override void Visit(SectionChunk chunk)
        {
        }
        protected override void Visit(TypeMemberChunk chunk)
        {
        }
        protected override void Visit(ResolveUrlChunk chunk)
        {
        }
        protected override void Visit(SetBaseTypeChunk chunk)
        {
        }
        protected override void Visit(TemplateChunk chunk)
        {
        }
        protected override void Visit(SetLayoutChunk chunk)
        {
        }
        protected override void Visit(SessionStateChunk chunk)
        {
        }
    }
}
