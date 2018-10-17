// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace TeamCityApi.Console.Commands
{
    internal class RootCommand : CommandBase
    {
        protected override void ConfigureDefaultOptions(CommandLineApplication application)
        {
        }

        protected override void ConfigureCore(CommandLineApplication application)
        {
            application.FullName = "teamcityapi";

            application.Command("all-statistics", new AllStatisticsCommand().Configure);
            application.Command("build-statistics", new BuildStatisticsCommand().Configure);
            application.Command("test-statistics", new TestStatisticsCommand().Configure);

            application.VersionOption("--version", GetVersion);
        }

        private static string GetVersion()
               => typeof(RootCommand).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override bool IsValid()
        {
            return true;
        }
    }
}
