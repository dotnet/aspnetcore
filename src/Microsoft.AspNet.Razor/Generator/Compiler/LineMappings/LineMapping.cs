
namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMapping
    {
        public LineMapping()
            : this(documentLocation: null, generatedLocation: null)
        {
        }

        public LineMapping(MappingLocation documentLocation, MappingLocation generatedLocation)
        {
            DocumentLocation = documentLocation;
            GeneratedLocation = generatedLocation;
        }

        public MappingLocation DocumentLocation { get; set; }
        public MappingLocation GeneratedLocation { get; set; }

        public override bool Equals(object obj)
        {
            LineMapping other = obj as LineMapping;
            return DocumentLocation.Equals(other.DocumentLocation) &&
                   GeneratedLocation.Equals(other.GeneratedLocation);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(LineMapping left, LineMapping right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LineMapping left, LineMapping right)
        {
            return !left.Equals(right);
        }
    }
}
