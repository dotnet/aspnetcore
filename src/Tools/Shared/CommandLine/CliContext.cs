// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Tools.Internal
{
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
}
