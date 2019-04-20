// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public static class DeployerSelector
    {
        public static ServerType ServerType => ServerType.IIS;
        public static bool IsForwardsCompatibilityTest => false;
        public static bool HasNewShim => false;
        public static bool HasNewHandler => true;
    }
}
