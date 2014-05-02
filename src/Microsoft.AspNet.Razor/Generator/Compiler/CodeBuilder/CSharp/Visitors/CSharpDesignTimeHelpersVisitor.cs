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

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpDesignTimeHelpersVisitor : CodeVisitor<CSharpCodeWriter>
    {
        internal const string InheritsHelper = "__inheritsHelper";
        internal const string DesignTimeHelperMethodName = "__RazorDesignTimeHelpers__";

        public CSharpDesignTimeHelpersVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context) { }

        public void AcceptTree(CodeTree tree)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (Writer.BuildMethodDeclaration("private", "void", "@" + DesignTimeHelperMethodName))
                {
                    using (Writer.BuildDisableWarningScope())
                    {
                        Accept(tree.Chunks);
                    }
                }
            }
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (CSharpLineMappingWriter lineMappingWriter = Writer.BuildLineMapping(chunk.Start, chunk.TypeName.Length, Context.SourceFile))
                {
                    Writer.Indent(chunk.Start.CharacterIndex);

                    lineMappingWriter.MarkLineMappingStart();
                    Writer.Write(chunk.TypeName);
                    lineMappingWriter.MarkLineMappingEnd();

                    Writer.Write(" ").Write(InheritsHelper).Write(" = null;");
                }
            }
        }
    }
}
