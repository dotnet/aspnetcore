// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.Extensions.ApiDescription.Client.Properties;

namespace Microsoft.Extensions.ApiDescription.Client
{
    internal static class Json
    {
        public static CommandOption ConfigureOption(CommandLineApplication command)
            => command.Option("--json", Resources.JsonDescription);

        public static string Literal(string text)
            => text != null
                ? "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
                : "null";
    }
}
