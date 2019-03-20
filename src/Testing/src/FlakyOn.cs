using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Testing
{
    public static class FlakyOn
    {
        public const string All = "All";

        public static class Helix
        {
            public const string All = QueuePrefix + "All";

            public const string Fedora28Amd64 = QueuePrefix + HelixQueues.Fedora28Amd64;
            public const string Fedora27Amd64 = QueuePrefix + HelixQueues.Fedora27Amd64;
            public const string Redhat7Amd64 = QueuePrefix + HelixQueues.Redhat7Amd64;
            public const string Debian9Amd64 = QueuePrefix + HelixQueues.Debian9Amd64;
            public const string Debian8Amd64 = QueuePrefix + HelixQueues.Debian8Amd64;
            public const string Centos7Amd64 = QueuePrefix + HelixQueues.Centos7Amd64;
            public const string Ubuntu1604Amd64 = QueuePrefix + HelixQueues.Ubuntu1604Amd64;
            public const string Ubuntu1810Amd64 = QueuePrefix + HelixQueues.Ubuntu1810Amd64;
            public const string macOS1012Amd64 = QueuePrefix + HelixQueues.macOS1012Amd64;
            public const string Windows10Amd64 = QueuePrefix + HelixQueues.Windows10Amd64;

            private const string Prefix = "Helix:";
            private const string QueuePrefix = Prefix + "Queue:";
        }

        public static class AzP
        {
            public const string All = Prefix + "All";
            public const string Windows = OsPrefix + "Windows_NT";
            public const string macOS = OsPrefix + "Darwin";
            public const string Linux = OsPrefix + "Linux";

            private const string Prefix = "AzP:";
            private const string OsPrefix = Prefix + "OS:";
        }
    }
}
