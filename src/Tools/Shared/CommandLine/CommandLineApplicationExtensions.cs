// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CommandLineUtils;

internal static class CommandLineApplicationExtensions
{
    public static CommandOption HelpOption(this CommandLineApplication app)
        => app.HelpOption("-?|-h|--help");

    public static CommandOption VerboseOption(this CommandLineApplication app)
        => app.Option("-v|--verbose", "Show verbose output", CommandOptionType.NoValue, inherited: true);

    public static void OnExecute(this CommandLineApplication app, Action action)
        => app.OnExecute(() =>
            {
                action();
                return 0;
            });

    public static CommandOption Option(this CommandLineApplication command, string template, string description)
        => command.Option(
            template,
            description,
#if NETCOREAPP
            template.Contains('<')
#else
            template.IndexOf('<') != -1
#endif
                ? template.EndsWith(">...", StringComparison.Ordinal) ? CommandOptionType.MultipleValue : CommandOptionType.SingleValue
                : CommandOptionType.NoValue);

    public static void VersionOptionFromAssemblyAttributes(this CommandLineApplication app)
        => app.VersionOptionFromAssemblyAttributes(typeof(CommandLineApplicationExtensions).Assembly);

    public static void VersionOptionFromAssemblyAttributes(this CommandLineApplication app, Assembly assembly)
        => app.VersionOption("--version", GetInformationalVersion(assembly));

    private static string GetInformationalVersion(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        var versionAttribute = attribute == null
            ? assembly.GetName().Version.ToString()
            : attribute.InformationalVersion;

        return versionAttribute;
    }
}
