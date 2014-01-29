using System.IO;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IMvcRazorHost
    {
        GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream);
    }
}
