using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class ChunkBlock : Chunk
    {
        public ChunkBlock()
        {
            Children = new List<Chunk>();
        }

        public IList<Chunk> Children { get; set; }
    }
}
