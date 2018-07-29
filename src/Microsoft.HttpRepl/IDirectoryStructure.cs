using System.Collections.Generic;

namespace Microsoft.HttpRepl
{
    public interface IDirectoryStructure
    {
        IEnumerable<string> DirectoryNames { get; }

        IDirectoryStructure Parent { get; }

        IDirectoryStructure GetChildDirectory(string name);

        IRequestInfo RequestInfo { get; }
    }

    public interface IRequestInfo
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> ContentTypesByMethod { get; }

        IReadOnlyList<string> Methods { get; }

        string GetRequestBodyForContentType(string contentType, string method);
    }
}
