// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RunTests
{
    public class RunTestsOptions
    {
        public static RunTestsOptions Parse(string[] args)
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
                    aliases: new string[] { "--quarantined" },
                    description: "Whether quarantined tests should run or not")
                { Argument = new Argument<bool>(), Required = true },

                new Option(
                    aliases: new string[] { "--ef" },
                    description: "The version of the EF tool to use")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--aspnetruntime" },
                    description: "The path to the aspnet runtime nupkg to install")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--aspnetref" },
                    description: "The path to the aspnet ref nupkg to install")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--helixTimeout" },
                    description: "The timeout duration of the Helix job")
                { Argument = new Argument<string>(), Required = true },
            };

            var parseResult = command.Parse(args);
            var options = new RunTestsOptions();
            options.Target = parseResult.ValueForOption<string>("--target");
            options.RuntimeVersion = parseResult.ValueForOption<string>("--runtime");
            options.HelixQueue = parseResult.ValueForOption<string>("--queue");
            options.Architecture = parseResult.ValueForOption<string>("--arch");
            options.Quarantined = parseResult.ValueForOption<bool>("--quarantined");
            options.EfVersion = parseResult.ValueForOption<string>("--ef");
            options.AspNetRuntime = parseResult.ValueForOption<string>("--aspnetruntime");
            options.AspNetRef = parseResult.ValueForOption<string>("--aspnetref");
            options.Timeout = TimeSpan.Parse(parseResult.ValueForOption<string>("--helixTimeout"));
            options.HELIX_WORKITEM_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");
            options.Path = Environment.GetEnvironmentVariable("PATH");
            options.DotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            return options;
        }

        public string Target { get; set;}
        public string SdkVersion { get; set;}
        public string RuntimeVersion { get; set;}
        public string AspNetRuntime { get; set;}
        public string AspNetRef { get; set;}
        public string HelixQueue { get; set;}
        public string Architecture { get; set;}
        public bool Quarantined { get; set;}
        public string EfVersion { get; set;}
        public string HELIX_WORKITEM_ROOT { get; set;}
        public string DotnetRoot { get; set; }
        public string Path { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}
