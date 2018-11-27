// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static partial class BuildVariables
    {
        private static string _msBuildPath = string.Empty;
        private static string _microsoftNETCoreApp30PackageVersion = string.Empty;
        private static string _netStandardLibrary20PackageVersion = string.Empty;

        static partial void InitializeVariables();

        public static string MSBuildPath
        {
            get
            {
                InitializeVariables();
                return _msBuildPath;
            }
        }

        public static string MicrosoftNETCoreApp30PackageVersion
        {
            get
            {
                InitializeVariables();
                return _microsoftNETCoreApp30PackageVersion;
            }
        }

        public static string NETStandardLibrary20PackageVersion
        {
            get
            {
                InitializeVariables();
                return _netStandardLibrary20PackageVersion;
            }
        }
    }
}
