using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.RateLimits
{
    public readonly struct MetadataName : IEquatable<MetadataName>
    {
        // Value is a TimeSpan
        public static MetadataName RetryAfter { get { return new MetadataName("RetryAfter"); } }
        // Value is a string
        public static MetadataName ReasonPhrase { get { return new MetadataName("ReasonPhrase"); } }

        private readonly string? _name;

        public MetadataName(string? name)
        {
            _name = name;
        }

        public string? Name
        {
            get { return _name; }
        }

        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        public override int GetHashCode()
        {
            return _name == null ? 0 : _name.GetHashCode();
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is MetadataName && Equals((MetadataName)obj);
        }

        public bool Equals(MetadataName other)
        {
            // NOTE: intentionally ordinal and case sensitive, matches CNG.
            return _name == other._name;
        }

        public static bool operator ==(MetadataName left, MetadataName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataName left, MetadataName right)
        {
            return !(left == right);
        }
    }
}
