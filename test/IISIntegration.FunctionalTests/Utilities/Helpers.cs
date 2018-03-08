// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static string GetInProcessTestSitesPath()
        {
            return Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "..", // tfm
                "..", // debug
                "..", // obj
                "..", // projectfolder
                "IISTestSite"));
        }

        public static string GetOutOfProcessTestSitesPath()
        {
            return Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "..", // tfm
                "..", // debug
                "..", // obj
                "..", // projectfolder
                "TestSites"));
        }
    }
}
