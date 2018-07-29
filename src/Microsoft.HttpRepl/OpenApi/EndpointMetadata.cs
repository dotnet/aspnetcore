using System.Collections.Generic;

namespace Microsoft.HttpRepl.OpenApi
{
    public class EndpointMetadata
    {
        public EndpointMetadata(string path, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>> requestsByMethodAndContentType)
        {
            Path = path;
            AvailableRequests = requestsByMethodAndContentType ?? new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>>();
        }

        public string Path { get; }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>> AvailableRequests { get; }
    }
}