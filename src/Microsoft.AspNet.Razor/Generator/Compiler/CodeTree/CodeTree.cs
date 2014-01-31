using System.Collections.Generic;

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
