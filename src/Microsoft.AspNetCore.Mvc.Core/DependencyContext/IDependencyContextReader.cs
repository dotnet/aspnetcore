using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal interface IDependencyContextReader
    {
        DependencyContext Read(Stream stream);
    }
}