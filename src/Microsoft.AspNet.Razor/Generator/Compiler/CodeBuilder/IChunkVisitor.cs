using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public interface IChunkVisitor
    {
        void Accept(IList<Chunk> chunks);
        void Accept(Chunk chunk);
    }
}
