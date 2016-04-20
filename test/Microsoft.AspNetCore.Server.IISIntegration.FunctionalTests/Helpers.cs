// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Server.Testing;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static string GetTestSitesPath(ApplicationType applicationType)
        {
            return Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", applicationType == ApplicationType.Standalone ? "TestSites" : "TestSites.Portable"));
        }
    }
}