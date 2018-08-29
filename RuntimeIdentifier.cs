// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cli.FunctionalTests
{
    // https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    public class RuntimeIdentifier
    {
        public static RuntimeIdentifier None = new RuntimeIdentifier() {
            Name = "none",
            OSPlatforms = new[] { OSPlatform.Linux, OSPlatform.OSX, OSPlatform.Windows, },
        };

        public static RuntimeIdentifier Linux_x64 = new RuntimeIdentifier() {
            Name = "linux-x64",
            OSPlatforms = new[] { OSPlatform.Linux, },
            ExecutableFileExtension = string.Empty,
        };

        public static RuntimeIdentifier OSX_x64 = new RuntimeIdentifier()
        {
            Name = "osx-x64",
            OSPlatforms = new[] { OSPlatform.OSX, },
            ExecutableFileExtension = string.Empty,
        };

        public static RuntimeIdentifier Win_x64 = new RuntimeIdentifier()
        {
            Name = "win-x64",
            OSPlatforms = new[] { OSPlatform.Windows, },
            ExecutableFileExtension = ".exe",
        };

        public static IEnumerable<RuntimeIdentifier> All = new[]
        {
            RuntimeIdentifier.None,
            RuntimeIdentifier.Linux_x64,
            RuntimeIdentifier.OSX_x64,
            RuntimeIdentifier.Win_x64,
        };

        private RuntimeIdentifier() { }

        public string Name { get; private set; }
        public string RuntimeArgument => (this == None) ? string.Empty : $"--runtime {Name}";
        public string Path => (this == None) ? string.Empty : Name;
        public IEnumerable<OSPlatform> OSPlatforms { get; private set; }
        public string ExecutableFileExtension { get; private set; }

        public override string ToString() => Name;
    }
}
