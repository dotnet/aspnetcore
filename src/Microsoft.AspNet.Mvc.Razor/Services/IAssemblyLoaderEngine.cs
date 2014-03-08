using System.Reflection;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface IAssemblyLoaderEngine
    {
        Assembly LoadFile(string path);
        Assembly LoadBytes(byte[] assemblyBytes, byte[] pdbBytes);
    }
}
