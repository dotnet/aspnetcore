using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class MappingLocation
    {
        public MappingLocation() : base() { }

        public MappingLocation(SourceLocation location, int contentLength)
        {
            ContentLength = contentLength;
            AbsoluteIndex = location.AbsoluteIndex;
            LineIndex = location.LineIndex;
            CharacterIndex = location.CharacterIndex;
        }

        public int ContentLength { get; set; }
        public int AbsoluteIndex { get; set; }
        public int LineIndex { get; set; }
        public int CharacterIndex { get; set; }
    }
}
