using System.Collections.Generic;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface ILibraryInformation
    {
        string Name { get; }

        IEnumerable<string> Dependencies { get; }
    }
}