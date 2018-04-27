using AspNetCoreSdkTests.Templates;
using System.Collections.Generic;

namespace AspNetCoreSdkTests.Util
{
    public class DotNetContext : TempDir
    {
        public string New(Template template)
        {
            return DotNet.New(template.Name, Path);
        }

        public string Restore(NuGetConfig config)
        {
            return DotNet.Restore(Path, config);
        }

        public string Build()
        {
            return DotNet.Build(Path);
        }

        public IEnumerable<string> GetObjFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "obj"));
        }

        public IEnumerable<string> GetBinFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "bin"));
        }
    }
}
