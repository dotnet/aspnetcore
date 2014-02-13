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

        public override bool Equals(object obj)
        {
            MappingLocation other = obj as MappingLocation;

            return AbsoluteIndex == other.AbsoluteIndex &&
                   ContentLength == other.ContentLength &&
                   LineIndex == other.LineIndex &&
                   CharacterIndex == other.CharacterIndex;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MappingLocation left, MappingLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MappingLocation left, MappingLocation right)
        {
            return !left.Equals(right);
        }
    }
}
