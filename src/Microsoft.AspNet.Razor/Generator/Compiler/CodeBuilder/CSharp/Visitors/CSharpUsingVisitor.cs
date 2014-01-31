using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    // TODO: This class shares a lot of the same properties as the other CSharpCodeVisitor, make common base?
    public class CSharpUsingVisitor : CodeVisitor
    {
        private CSharpCodeWriter _writer;
        private string _sourceFile;

        public CSharpUsingVisitor(CSharpCodeWriter writer, string sourceFile)
        {
            _writer = writer;
            _sourceFile = sourceFile;
            ImportedUsings = new List<string>();
        }

        public IList<string> ImportedUsings { get; set; }

        protected override void Visit(UsingChunk chunk)
        {
            using (_writer.BuildLineMapping(chunk.Start, chunk.Association.Length, _sourceFile))
            {
                ImportedUsings.Add(chunk.Namespace);
                _writer.WriteUsing(chunk.Namespace);
            }
        }
    }
}
