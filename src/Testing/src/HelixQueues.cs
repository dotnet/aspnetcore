namespace Microsoft.AspNetCore.Testing
{
    public static class HelixQueues
    {
        public const string Fedora28Amd64 = "Fedora.28." + Amd64Suffix;
        public const string Fedora27Amd64 = "Fedora.27." + Amd64Suffix;
        public const string Redhat7Amd64 = "Redhat.7." + Amd64Suffix;
        public const string Debian9Amd64 = "Debian.9." + Amd64Suffix;
        public const string Debian8Amd64 = "Debian.8." + Amd64Suffix;
        public const string Centos7Amd64 = "Centos.7." + Amd64Suffix;
        public const string Ubuntu1604Amd64 = "Ubuntu.1604." + Amd64Suffix;
        public const string Ubuntu1810Amd64 = "Ubuntu.1810." + Amd64Suffix;
        public const string macOS1012Amd64 = "OSX.1012." + Amd64Suffix;
        public const string Windows10Amd64 = "Windows.10.Amd64.ClientRS4.VS2017.Open"; // Doesn't have the default suffix!

        private const string Amd64Suffix = "Amd64.Open";
    }
}
