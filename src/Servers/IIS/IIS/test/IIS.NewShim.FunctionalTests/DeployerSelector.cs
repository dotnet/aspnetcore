// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public static class DeployerSelector
{
    public static ServerType ServerType => ServerType.IIS;
    public static bool IsNewShimTest => true;
    public static bool HasNewShim => true;
    public static bool HasNewHandler => false;
}
