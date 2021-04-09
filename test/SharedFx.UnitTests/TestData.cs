// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore
{
    public class TestData
    {
        public static string GetPackageVersion() => GetTestDataValue("PackageVersion");

        public static string GetPreviousAspNetCoreReleaseVersion() => GetTestDataValue("PreviousAspNetCoreReleaseVersion");

        public static string GetMicrosoftNETCoreAppPackageVersion() => GetTestDataValue("MicrosoftNETCoreAppPackageVersion");

        public static string GetDotNetRoot() => GetTestDataValue("DotNetRoot");

        public static string GetRepositoryCommit() => GetTestDataValue("RepositoryCommit");

        public static string GetSharedFxRuntimeIdentifier() => GetTestDataValue("SharedFxRuntimeIdentifier");

        public static bool GetValidateBaseline() =>
            string.Equals(GetTestDataValue("ValidateBaseline"), "true", StringComparison.OrdinalIgnoreCase);

        private static string GetTestDataValue(string key)
             => typeof(TestData).Assembly.GetCustomAttributes<TestDataAttribute>().Single(d => d.Key == key).Value;
    }
}
