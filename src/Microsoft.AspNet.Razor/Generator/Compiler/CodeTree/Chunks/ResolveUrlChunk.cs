using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class ResolveUrlChunk : Chunk
    {
        public string Url { get; set; }
        public ExpressionRenderingMode RenderingMode { get; set; }
    }
}
