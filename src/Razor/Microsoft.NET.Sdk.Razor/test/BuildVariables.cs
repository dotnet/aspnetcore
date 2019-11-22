// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static partial class BuildVariables
    {
        private static string _msBuildPath = string.Empty;
        private static string _microsoftNETCoreApp31PackageVersion = string.Empty;
        private static string _microsoftNetCompilersToolsetPackageVersion = string.Empty;

        static partial void InitializeVariables();

        public static string MSBuildPath
        {
            get
            {
                InitializeVariables();
                return _msBuildPath;
            }
        }

        public static string MicrosoftNETCoreApp31PackageVersion
        {
            get
            {
                InitializeVariables();
                return _microsoftNETCoreApp31PackageVersion;
            }
        }

        public static string MicrosoftNetCompilersToolsetPackageVersion
        {
            get
            {
                InitializeVariables();
                return _microsoftNetCompilersToolsetPackageVersion;
            }
        }
    }
}
