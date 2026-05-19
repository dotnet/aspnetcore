// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public static class CliContext
{
    /// <summary>
    /// dotnet -d|--diagnostics subcommand
    /// </summary>
    /// <returns></returns>
    public static bool IsGlobalVerbose()
    {
        bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE"), out bool globalVerbose);
        return globalVerbose;
    }
}
