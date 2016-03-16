using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal class RuntimeTarget
    {
        public RuntimeTarget(string runtime, IEnumerable<RuntimeAssembly> assemblies, IEnumerable<string> nativeLibraries)
        {
            Runtime = runtime;
            Assemblies = assemblies.ToArray();
            NativeLibraries = nativeLibraries.ToArray();
        }

        public string Runtime { get; }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeLibraries { get; }
    }
}