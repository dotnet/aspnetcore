// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Dnx.Runtime
{
    internal static class Constants
    {
        public const string BootstrapperExeName = "dnx";
        public const string BootstrapperFullName = "Microsoft .NET Execution environment";
        public const string DefaultLocalRuntimeHomeDir = ".dnx";
        public const string RuntimeShortName = "dnx";
        public const string RuntimeLongName = "Microsoft DNX";
        public const string RuntimeNamePrefix = RuntimeShortName + "-";
        public const string WebConfigRuntimeVersion = RuntimeNamePrefix + "version";
        public const string WebConfigRuntimeFlavor = RuntimeNamePrefix + "clr";
        public const string WebConfigRuntimeAppBase = RuntimeNamePrefix + "app-base";
        public const string WebConfigBootstrapperVersion = "bootstrapper-version";
        public const string WebConfigRuntimePath = "runtime-path";
        public const string BootstrapperHostName = RuntimeShortName + ".host";
        public const string BootstrapperClrName = RuntimeShortName + ".clr";

        public const int LockFileVersion = 2;

        public static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(10);
        public static readonly string AppConfigurationFileName = "app.config";

        public static readonly Version Version35 = new Version(3, 5);
        public static readonly Version Version40 = new Version(4, 0);
        public static readonly Version Version50 = new Version(5, 0);
        public static readonly Version Version10_0 = new Version(10, 0);
    }
}
