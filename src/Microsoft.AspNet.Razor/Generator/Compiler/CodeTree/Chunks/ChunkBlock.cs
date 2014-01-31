using System.Collections.Generic;

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
