using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public interface IEndpointMetadataReader
    {
        bool CanHandle(JObject document);

        IEnumerable<EndpointMetadata> ReadMetadata(JObject document);
    }
}