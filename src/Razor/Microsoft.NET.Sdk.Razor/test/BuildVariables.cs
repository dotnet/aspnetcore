// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static partial class BuildVariables
    {
        private static string _msBuildPath = string.Empty;
        private static string _MicrosoftNETCoreApp50PackageVersion = string.Empty;
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

        public static string MicrosoftNETCoreApp50PackageVersion
        {
            get
            {
                InitializeVariables();
                return _MicrosoftNETCoreApp50PackageVersion;
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
