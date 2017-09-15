// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorParserOptions
    {
        internal static readonly RazorLanguageVersion LatestRazorLanguageVersion = RazorLanguageVersion.Version2_1;

        public static RazorParserOptions CreateDefault()
        {
            return new DefaultRazorParserOptions(
                Array.Empty<DirectiveDescriptor>(),
                designTime: false,
                parseLeadingDirectives: false,
                version: LatestRazorLanguageVersion);
        }

        public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorParserOptionsBuilder(designTime: false, version: LatestRazorLanguageVersion);
            configure(builder);
            var options = builder.Build();

            return options;
        }

        public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorParserOptionsBuilder(designTime: true, version: LatestRazorLanguageVersion);
            configure(builder);
            var options = builder.Build();

            return options;
        }

        public abstract bool DesignTime { get; }

        public abstract IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

        /// <summary>
        /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
        /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
        /// </summary>
        /// <remarks>
        /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
        /// In a future release this may be updated to include all leading directive content.
        /// </remarks>
        public abstract bool ParseLeadingDirectives { get; }

        public virtual RazorLanguageVersion Version { get; } = LatestRazorLanguageVersion;

        internal virtual RazorParserFeatureFlags FeatureFlags { get; }
    }
}
