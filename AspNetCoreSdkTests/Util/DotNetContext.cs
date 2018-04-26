namespace AspNetCoreSdkTests.Util
{
    public class DotNetContext : TempDir
    {
        public string New(Template template, bool restore)
        {
            return DotNet.New(template.ToString(), Path, restore);
        }

        public string Restore(NuGetConfig config)
        {
            return DotNet.Restore(Path, config);
        }
    }
}
