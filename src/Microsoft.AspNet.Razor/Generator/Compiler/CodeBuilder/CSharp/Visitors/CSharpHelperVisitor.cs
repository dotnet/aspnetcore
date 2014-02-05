using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpHelperVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private CSharpCodeVisitor _codeVisitor;

        public CSharpHelperVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context)
        {
            _codeVisitor = new CSharpCodeVisitor(writer, context);
        }

        protected override void Visit(HelperChunk chunk)
        {
            IDisposable lambdaScope = null;

            using (CSharpLineMappingWriter mappingWriter = Writer.BuildLineMapping(chunk.Signature.Location, chunk.Signature.Value.Length, Context.SourceFile))
            {
                string accessibility = "public " + (Context.Host.StaticHelpers ? "static" : String.Empty);

                Writer.Write(accessibility).Write(" ").Write(Context.Host.GeneratedClassContext.TemplateTypeName).Write(" ");
                mappingWriter.MarkLineMappingStart();
                Writer.Write(chunk.Signature);
                mappingWriter.MarkLineMappingEnd();
            }

            if (chunk.HeaderComplete)
            {
                Writer.WriteStartReturn()
                       .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

                lambdaScope = Writer.BuildLambda(endLine: false, parameterNames: HelperWriterName);
            }

            string currentTargetWriterName = Context.TargetWriterName;
            Context.TargetWriterName = HelperWriterName;

            // Generate children code
            _codeVisitor.Accept(chunk.Children);

            Context.TargetWriterName = currentTargetWriterName;

            if (chunk.HeaderComplete)
            {
                lambdaScope.Dispose();
                Writer.WriteEndMethodInvocation();
            }

            if (chunk.Footer != null && !String.IsNullOrEmpty(chunk.Footer.Value))
            {
                using (Writer.BuildLineMapping(chunk.Footer.Location, chunk.Footer.Value.Length, Context.SourceFile))
                {
                    Writer.Write(chunk.Footer);
                }
            }

            Writer.WriteLine();
        }
    }
}
