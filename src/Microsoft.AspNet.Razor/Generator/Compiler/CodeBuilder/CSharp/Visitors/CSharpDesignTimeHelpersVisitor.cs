// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpDesignTimeHelpersVisitor : CodeVisitor<CSharpCodeWriter>
    {
        internal const string InheritsHelper = "__inheritsHelper";
        internal const string DesignTimeHelperMethodName = "__RazorDesignTimeHelpers__";

        private const int DisableVariableNamingWarnings = 219;

        public CSharpDesignTimeHelpersVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context) { }

        public void AcceptTree(CodeTree tree)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (Writer.BuildMethodDeclaration("private", "void", "@" + DesignTimeHelperMethodName))
                {
                    using (Writer.BuildDisableWarningScope(DisableVariableNamingWarnings))
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
