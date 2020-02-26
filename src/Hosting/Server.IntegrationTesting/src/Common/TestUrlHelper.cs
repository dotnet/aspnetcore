// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common
{
    // Public for use in other test projects
    public static class TestUrlHelper
    {
        public static string GetTestUrl(ServerType serverType)
        {
            return TestUriHelper.BuildTestUri(serverType).ToString();
        }
    }
}
