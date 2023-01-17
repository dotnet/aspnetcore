// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HelixTestRunner;

public class HelixTestRunnerOptions
{
    public static HelixTestRunnerOptions Parse(string[] args)
    {
        var command = new RootCommand()
        {
            new Option(
                aliases: new string[] { "--target", "-t" },
                description: "The test dll to run")
                { Argument = new Argument<string>(), Required = true },

            new Option(
                aliases: new string[] { "--runtime" },
                description: "The version of the ASP.NET runtime being installed and used")
            { Argument = new Argument<string>(), Required = true },

            new Option(
                aliases: new string[] { "--queue" },
                description: "The name of the Helix queue being run on")
            { Argument = new Argument<string>(), Required = true },

            new Option(
                aliases: new string[] { "--arch" },
                description: "The architecture being run on")
            { Argument = new Argument<string>(), Required = true },

            new Option(
                aliases: new string[] { "--playwright" },
                description: "Whether to install Microsoft.Playwright browsers or not")
            { Argument = new Argument<bool>(), Required = true },

            new Option(
                aliases: new string[] { "--quarantined" },
                description: "Whether quarantined tests should run or not")
            { Argument = new Argument<bool>(), Required = true },

            new Option(
                aliases: new string[] { "--helixTimeout" },
                description: "The timeout duration of the Helix job")
            { Argument = new Argument<string>(), Required = true },

            new Option(
                aliases: new string[] { "--source" },
                description: "The restore sources to use during testing")
            { Argument = new Argument<string>() { Arity = ArgumentArity.ZeroOrMore }, Required = true }
        };

        var parseResult = command.Parse(args);
        var sharedFxVersion = parseResult.ValueForOption<string>("--runtime");
        var options = new HelixTestRunnerOptions
        {
            Architecture = parseResult.ValueForOption<string>("--arch"),
            HelixQueue = parseResult.ValueForOption<string>("--queue"),
            InstallPlaywright = parseResult.ValueForOption<bool>("--playwright"),
            Quarantined = parseResult.ValueForOption<bool>("--quarantined"),
            RuntimeVersion = sharedFxVersion,
            Target = parseResult.ValueForOption<string>("--target"),
            Timeout = TimeSpan.Parse(parseResult.ValueForOption<string>("--helixTimeout"), CultureInfo.InvariantCulture),

            // When targeting pack builds, it has exactly the same version as the shared framework.
            AspNetRef = $"Microsoft.AspNetCore.App.Ref.{sharedFxVersion}.nupkg",
            AspNetRuntime = $"Microsoft.AspNetCore.App.Runtime.win-x64.{sharedFxVersion}.nupkg",

            DotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            HELIX_WORKITEM_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"),
            Path = Environment.GetEnvironmentVariable("PATH"),
        };

        return options;
    }

    public string Architecture { get; private set; }
    public string HelixQueue { get; private set; }
    public bool InstallPlaywright { get; private set; }
    public bool Quarantined { get; private set; }
    public string RuntimeVersion { get; private set; }
    public string Target { get; private set; }
    public TimeSpan Timeout { get; private set; }

    public string AspNetRef { get; private set; }
    public string AspNetRuntime { get; private set; }
    public string HELIX_WORKITEM_ROOT { get; private set; }
    public string DotnetRoot { get; private set; }
    public string Path { get; set; }
}
