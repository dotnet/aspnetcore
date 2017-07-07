// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCodeGenerationOptions
    {
        public static RazorCodeGenerationOptions CreateDefault()
        {
            return new DefaultRazorCodeGenerationOptions(indentWithTabs: false, indentSize: 4, designTime: false, suppressChecksum: false);
        }

        public static RazorCodeGenerationOptions CreateDesignTimeDefault()
        {
            return new DefaultRazorCodeGenerationOptions(indentWithTabs: false, indentSize: 4, designTime: true, suppressChecksum: false);
        }

        public static RazorCodeGenerationOptions Create(Action<RazorCodeGenerationOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorCodeGenerationOptionsBuilder(designTime: false);
            configure(builder);
            var options = builder.Build();

            return options;
        }

        public static RazorCodeGenerationOptions CreateDesignTime(Action<RazorCodeGenerationOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorCodeGenerationOptionsBuilder(designTime: true);
            configure(builder);
            var options = builder.Build();

            return options;
        }

        public abstract bool DesignTime { get; }

        public abstract bool IndentWithTabs { get; }

        public abstract int IndentSize { get; }

        public abstract bool SuppressChecksum { get; }
    }
}
