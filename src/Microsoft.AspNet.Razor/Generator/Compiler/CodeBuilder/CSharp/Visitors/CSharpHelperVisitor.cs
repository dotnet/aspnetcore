using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpHelperVisitor : CodeVisitor
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private CSharpCodeWriter _writer;
        private string _sourceFile;
        private RazorEngineHost _host;
        private CSharpCodeVisitor _codeVisitor;

        public CSharpHelperVisitor(CSharpCodeWriter writer, RazorEngineHost host, string sourceFile)
        {
            _writer = writer;
            _sourceFile = sourceFile;
            _host = host;
            _codeVisitor = new CSharpCodeVisitor(writer, host, sourceFile);
        }

        protected override void Visit(HelperChunk chunk)
        {
            IDisposable lambdaScope = null;

            using (CSharpLineMappingWriter mappingWriter = _writer.BuildLineMapping(chunk.Signature.Location, chunk.Signature.Value.Length, _sourceFile))
            {
                string accessibility = "public " + (_host.StaticHelpers ? "static" : String.Empty);

                _writer.Write(accessibility).Write(" ").Write(_host.GeneratedClassContext.TemplateTypeName).Write(" ");
                mappingWriter.MarkLineMappingStart();
                _writer.Write(chunk.Signature);
                mappingWriter.MarkLineMappingEnd();
            }

            if(chunk.HeaderComplete)
            {
                _writer.WriteStartReturn()
                       .WriteStartNewObject(_host.GeneratedClassContext.TemplateTypeName);

                lambdaScope = _writer.BuildLambda(endLine: false, parameterNames: HelperWriterName);
            }
            
            // Generate children code
            _codeVisitor.Accept(chunk.Children);

            if (chunk.HeaderComplete)
            {
                lambdaScope.Dispose();
                _writer.WriteEndMethodInvocation();
            }

            if(chunk.Footer != null && !String.IsNullOrEmpty(chunk.Footer.Value))
            {
                using(_writer.BuildLineMapping(chunk.Footer.Location, chunk.Footer.Value.Length, _sourceFile))
                {
                    _writer.Write(chunk.Footer);
                }
            }

            _writer.WriteLine();
        }
    }
}
