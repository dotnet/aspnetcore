using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Testing
{
    public static class AzurePipelines
    {
        public const string All = Prefix + "All";
        public const string Windows = OsPrefix + "Windows_NT";
        public const string macOS = OsPrefix + "Darwin";
        public const string Linux = OsPrefix + "Linux";

        private const string Prefix = "AzP:";
        private const string OsPrefix = Prefix + "OS:";
    }
}
