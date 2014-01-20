using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public interface IChunkVisitor
    {
        void Accept(IList<Chunk> chunks);
        void Accept(Chunk chunk);
    }
}
