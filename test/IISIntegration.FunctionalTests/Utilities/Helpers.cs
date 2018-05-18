// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static string GetTestWebSitePath(string name)
        {
            return Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"),"test", "WebSites", name);
        }

        public static string GetInProcessTestSitesPath() => GetTestWebSitePath("InProcessWebSite");

        public static string GetOutOfProcessTestSitesPath() => GetTestWebSitePath("OutOfProcessWebSite");
    }
}
