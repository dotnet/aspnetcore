
namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface IMetadataFileReference : IMetadataReference
    {
        string Path { get; }
    }
}
