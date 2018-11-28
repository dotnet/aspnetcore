// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal class CompilationOptions
    {
        public static readonly string ConfigureCompilationTypeTemplate = "--configure-compilation-type";
        public static readonly string ContentRootTemplate = "--content-root";
        public static readonly string EmbedViewSourceTemplate = "--embed-view-sources";
        public static readonly string StrongNameKeyPath = "--key-file";
        public static readonly string DelaySignTemplate = "--delay-sign";
        public static readonly string PublicSignTemplate = "--public-sign";
        public static readonly string ApplicationNameTemplate = "--application-name";
        public static readonly string OutputPathTemplate = "--output-path";
        public static readonly string ViewsToCompileTemplate = "--file";

        public CompilationOptions(CommandLineApplication app)
        {
            OutputPathOption = app.Option(
               OutputPathTemplate,
                "Path to the emit the precompiled assembly to.",
                CommandOptionType.SingleValue);

            ApplicationNameOption = app.Option(
                ApplicationNameTemplate,
                "Name of the application to produce precompiled assembly for.",
                CommandOptionType.SingleValue);

            ProjectArgument = app.Argument(
                "project",
                "The path to the project file.");

            ConfigureCompilationType = app.Option(
                ConfigureCompilationTypeTemplate,
                "Type with Configure method",
                CommandOptionType.SingleValue);

            ContentRootOption = app.Option(
                ContentRootTemplate,
                "The application's content root.",
                CommandOptionType.SingleValue);

            EmbedViewSourcesOption = app.Option(
                EmbedViewSourceTemplate,
                "Embed view sources as resources in the generated assembly.",
                CommandOptionType.NoValue);

            KeyFileOption = app.Option(
                StrongNameKeyPath,
                "Strong name key path.",
                CommandOptionType.SingleValue);

            DelaySignOption = app.Option(
                DelaySignTemplate,
                "Determines if the precompiled view assembly is to be delay signed.",
                CommandOptionType.NoValue);

            PublicSignOption = app.Option(
                PublicSignTemplate,
                "Determines if the precompiled view assembly is to be public signed.",
                CommandOptionType.NoValue);

            ViewsToCompileOption = app.Option(
                ViewsToCompileTemplate,
                "Razor files to compile.",
                CommandOptionType.MultipleValue);
        }

        public CommandArgument ProjectArgument { get; }

        public CommandOption ConfigureCompilationType { get; }

        public CommandOption ContentRootOption { get; }

        public CommandOption EmbedViewSourcesOption { get; }

        public CommandOption KeyFileOption { get; }

        public CommandOption DelaySignOption { get; }

        public CommandOption PublicSignOption { get; }

        public CommandOption OutputPathOption { get; }

        public CommandOption ApplicationNameOption { get; }

        public CommandOption ViewsToCompileOption { get; }

        public string OutputPath => OutputPathOption.Value();

        public string ApplicationName => ApplicationNameOption.Value();

        public string KeyFile => KeyFileOption.Value();

        public bool DelaySign => DelaySignOption.HasValue();

        public bool PublicSign => PublicSignOption.HasValue();

        public List<string> ViewsToCompile => ViewsToCompileOption.Values;
    }
}
