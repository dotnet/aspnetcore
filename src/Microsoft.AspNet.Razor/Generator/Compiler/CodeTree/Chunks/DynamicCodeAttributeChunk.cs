using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class DynamicCodeAttributeChunk : ChunkBlock
    {
        public LocationTagged<string> Prefix { get; set; }
    }
}
