using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTypeMemberVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpTypeMemberVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context) { }

        protected override void Visit(TypeMemberChunk chunk)
        {
            if (!String.IsNullOrEmpty(chunk.Code))
            {
                using (Writer.BuildLineMapping(chunk.Start, chunk.Code.Length, Context.SourceFile))
                {
                    Writer.WriteLine(chunk.Code);
                }
            }
        }
    }
}
