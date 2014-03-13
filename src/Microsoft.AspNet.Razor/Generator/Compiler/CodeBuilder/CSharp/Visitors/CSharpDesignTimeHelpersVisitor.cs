
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
