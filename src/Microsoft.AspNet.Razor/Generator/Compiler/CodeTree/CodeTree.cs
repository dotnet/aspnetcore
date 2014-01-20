using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeTree
    {
        public CodeTree()
        {
            Chunks = new List<Chunk>();
        }

        public IList<Chunk> Chunks { get; set; }
    }
}
