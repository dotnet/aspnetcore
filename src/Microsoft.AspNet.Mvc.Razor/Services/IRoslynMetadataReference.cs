using Microsoft.CodeAnalysis;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface IRoslynMetadataReference : IMetadataReference
    {
        MetadataReference MetadataReference { get; }
    }
}