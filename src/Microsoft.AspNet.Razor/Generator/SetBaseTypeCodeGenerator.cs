// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SetBaseTypeCodeGenerator : SpanCodeGenerator
    {
        public SetBaseTypeCodeGenerator(string baseType)
        {
            BaseType = baseType;
        }

        public string BaseType { get; private set; }

        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddSetBaseTypeChunk(target.Content, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            context.GeneratedClass.BaseTypes.Clear();
            context.GeneratedClass.BaseTypes.Add(new CodeTypeReference(ResolveType(context, BaseType.Trim())));

            if (context.Host.DesignTimeMode)
            {
                int generatedCodeStart = 0;
                string code = context.BuildCodeString(cw =>
                {
                    generatedCodeStart = cw.WriteVariableDeclaration(target.Content, "__inheritsHelper", null);
                    cw.WriteEndStatement();
                });

                int paddingCharCount;

                CodeSnippetStatement stmt = new CodeSnippetStatement(
                    CodeGeneratorPaddingHelper.Pad(context.Host, code, target, generatedCodeStart, out paddingCharCount))
                {
                    LinePragma = context.GenerateLinePragma(target, generatedCodeStart + paddingCharCount)
                };
                context.AddDesignTimeHelperStatement(stmt);
            }
#endif

            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        protected virtual string ResolveType(CodeGeneratorContext context, string baseType)
        {
            return baseType;
        }

        public override string ToString()
        {
            return "Base:" + BaseType;
        }

        public override bool Equals(object obj)
        {
            SetBaseTypeCodeGenerator other = obj as SetBaseTypeCodeGenerator;
            return other != null &&
                   String.Equals(BaseType, other.BaseType, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return BaseType.GetHashCode();
        }
    }
}
