using System.Collections.Generic;

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
