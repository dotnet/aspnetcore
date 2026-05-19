// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common;

// Public for use in other test projects
public static class TestUrlHelper
{
    public static string GetTestUrl(ServerType serverType)
    {
        return TestUriHelper.BuildTestUri(serverType).ToString();
    }
}
