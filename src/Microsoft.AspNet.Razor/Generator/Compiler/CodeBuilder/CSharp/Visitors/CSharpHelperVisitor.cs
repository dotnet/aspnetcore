using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpHelperVisitor : CodeVisitor
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private readonly CSharpCodeWriter _writer;
        private readonly CodeGeneratorContext _context;
        private CSharpCodeVisitor _codeVisitor;

        public CSharpHelperVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
        {
            _writer = writer;
            _context = context;
            _codeVisitor = new CSharpCodeVisitor(writer, context);
        }

        protected override void Visit(HelperChunk chunk)
        {
            IDisposable lambdaScope = null;

            using (CSharpLineMappingWriter mappingWriter = _writer.BuildLineMapping(chunk.Signature.Location, chunk.Signature.Value.Length, _context.SourceFile))
            {
                string accessibility = "public " + (_context.Host.StaticHelpers ? "static" : String.Empty);

                _writer.Write(accessibility).Write(" ").Write(_context.Host.GeneratedClassContext.TemplateTypeName).Write(" ");
                mappingWriter.MarkLineMappingStart();
                _writer.Write(chunk.Signature);
                mappingWriter.MarkLineMappingEnd();
            }

            if(chunk.HeaderComplete)
            {
                _writer.WriteStartReturn()
                       .WriteStartNewObject(_context.Host.GeneratedClassContext.TemplateTypeName);

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
                using(_writer.BuildLineMapping(chunk.Footer.Location, chunk.Footer.Value.Length, _context.SourceFile))
                {
                    _writer.Write(chunk.Footer);
                }
            }

            _writer.WriteLine();
        }
    }
}
