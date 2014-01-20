using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMapping
    {
        public MappingLocation DocumentLocation { get; set; }
        public MappingLocation GeneratedLocation { get; set; }
    }
}
