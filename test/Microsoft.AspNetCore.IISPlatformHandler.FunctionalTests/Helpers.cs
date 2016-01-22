// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.IISPlatformHandler.FunctionalTests
{
    public class Helpers
    {
        public static string GetTestSitesPath()
        {
            return Path.GetFullPath(Path.Combine("..", "TestSites"));
        }
    }
}