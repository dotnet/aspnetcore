using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpBaseTypeVisitor : CodeVisitor
    {
        private CSharpCodeWriter _writer;

        public CSharpBaseTypeVisitor(CSharpCodeWriter writer)
        {
            _writer = writer;
        }

        public string CurrentBaseType { get; set; }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            CurrentBaseType = chunk.TypeName;
        }
    }
}
