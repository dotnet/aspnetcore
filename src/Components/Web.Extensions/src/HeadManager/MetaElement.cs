namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class MetaElement
    {
        public string Name { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public override bool Equals(object? obj)
            => obj is MetaElement other && Name.Equals(other.Name) && Content.Equals(other.Content);

        public override int GetHashCode()
            => Name.GetHashCode() ^ Content.GetHashCode();
    }
}
