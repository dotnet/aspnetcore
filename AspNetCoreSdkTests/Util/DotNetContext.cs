using System.Collections.Generic;

namespace AspNetCoreSdkTests.Util
{
    public class DotNetContext : TempDir
    {
        public string New(Template template, bool restore)
        {
            return DotNet.New(template.ToString().ToLowerInvariant(), Path, restore);
        }

        public string Restore(NuGetConfig config)
        {
            return DotNet.Restore(Path, config);
        }

        public IEnumerable<string> GetObjFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "obj"));
        }
    }
}
