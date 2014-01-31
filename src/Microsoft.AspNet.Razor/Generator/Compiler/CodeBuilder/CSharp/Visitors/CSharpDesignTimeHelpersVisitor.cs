
namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpDesignTimeHelpersVisitor : CodeVisitor
    {
        internal const string InheritsHelper = "__inheritsHelper";

        private CSharpCodeWriter _writer;
        // TODO: No need for the entire host
        private RazorEngineHost _host;
        private string _sourceFile;

        public CSharpDesignTimeHelpersVisitor(CSharpCodeWriter writer, RazorEngineHost host, string sourceFile)
        {
            _writer = writer;
            _host = host;
            _sourceFile = sourceFile;
        }

        public void Accept(CodeTree tree)
        {
            if(_host.DesignTimeMode)
            {
                using(_writer.BuildMethodDeclaration("private","void", "@"+CodeGeneratorContext.DesignTimeHelperMethodName))
                {
                    using (_writer.BuildDisableWarningScope())
                    {
                        Accept(tree.Chunks);
                    }
                }
            }
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            if (_host.DesignTimeMode)
            {
                using (CSharpLineMappingWriter lineMappingWriter = _writer.BuildLineMapping(chunk.Start, chunk.TypeName.Length, _sourceFile))
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
