namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal readonly struct MetaElementKey
    {
        public MetaElementKeyName Name { get; }

        public string Id { get; }

        public MetaElementKey(MetaElementKeyName name, string id)
        {
            Name = name;
            Id = id;
        }

        public override bool Equals(object? obj)
            => obj is MetaElementKey other && Name.Equals(other.Name) && Id.Equals(other.Id);

        public override int GetHashCode()
            => Name.GetHashCode() ^ Id.GetHashCode();
    }
}
