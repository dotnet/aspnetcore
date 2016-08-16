// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal
{
    public class StrongNameOptions
    {
        public static readonly string StrongNameKeyPath = "--key-file";
        public static readonly string DelaySignTemplate = "--delay-sign";
        public static readonly string PublicSignTemplate = "--public-sign";

        public CommandOption KeyFileOption { get; set; }

        public CommandOption DelaySignOption { get; private set; }

        public CommandOption PublicSignOption { get; private set; }

        public void Configure(CommandLineApplication app)
        {
            KeyFileOption = app.Option(
                StrongNameKeyPath,
                "Strong name key path",
                CommandOptionType.SingleValue);

            DelaySignOption = app.Option(
                DelaySignTemplate,
                "Determines if the precompiled view assembly is to be delay signed.",
                CommandOptionType.NoValue);

            PublicSignOption = app.Option(
                PublicSignTemplate,
                "Determines if the precompiled view assembly is to be public signed.",
                CommandOptionType.NoValue);
        }

        public string KeyFile => KeyFileOption.Value();

        public bool DelaySign => DelaySignOption.HasValue();

        public bool PublicSign => PublicSignOption.HasValue();
    }
}
