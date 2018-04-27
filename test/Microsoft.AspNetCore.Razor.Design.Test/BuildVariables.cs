// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static partial class BuildVariables
    {
        private static string _msBuildPath = string.Empty;
        private static string _microsoftNETCoreAppVersion = string.Empty;

        private static string _netStandardLibraryVersion = string.Empty;

        static partial void InitializeVariables();

        public static string MSBuildPath
        {
            get
            {
                InitializeVariables();
                return _msBuildPath;
            }
        }

        public static string MicrosoftNETCoreAppVersion
        {
            get
            {
                InitializeVariables();
                return _microsoftNETCoreAppVersion;
            }
        }

        public static string NETStandardLibraryVersion
        {
            get
            {
                InitializeVariables();
                return _netStandardLibraryVersion;
            }
        }
    }
}
