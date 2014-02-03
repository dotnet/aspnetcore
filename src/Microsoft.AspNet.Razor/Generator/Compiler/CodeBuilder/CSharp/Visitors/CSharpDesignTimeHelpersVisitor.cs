
namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpDesignTimeHelpersVisitor : CodeVisitor
    {
        internal const string InheritsHelper = "__inheritsHelper";

        private readonly CSharpCodeWriter _writer;
        private readonly CodeGeneratorContext _context;

        public CSharpDesignTimeHelpersVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
        {
            _writer = writer;
            _context = context;
        }

        public override void Accept(System.Collections.Generic.IList<Chunk> chunks)
        {
            if (_context.Host.DesignTimeMode)
            {
                using (_writer.BuildMethodDeclaration("private", "void", "@" + CodeGeneratorContext.DesignTimeHelperMethodName))
                {
                    using (_writer.BuildDisableWarningScope())
                    {
                        Accept(chunks);
                    }
                }
            }
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            if (_context.Host.DesignTimeMode)
            {
                using (CSharpLineMappingWriter lineMappingWriter = _writer.BuildLineMapping(chunk.Start, chunk.TypeName.Length, _context.SourceFile))
                {
                    _writer.Indent(chunk.Start.CharacterIndex);

                    lineMappingWriter.MarkLineMappingStart();
                    _writer.Write(chunk.TypeName);
                    lineMappingWriter.MarkLineMappingEnd();

                    _writer.Write(" ").Write(InheritsHelper).Write(" = null;");
                }
            }
        }
    }
}
