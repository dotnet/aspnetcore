using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMappingManager
    {
        public LineMappingManager()
        {
            Mappings = new List<LineMapping>();
        }

        public List<LineMapping> Mappings { get; private set; }

        public void AddMapping(MappingLocation documentLocation, MappingLocation generatedLocation)
        {
            Mappings.Add(new LineMapping
            {
                DocumentLocation = documentLocation,
                GeneratedLocation = generatedLocation
            });
        }
    }
}
