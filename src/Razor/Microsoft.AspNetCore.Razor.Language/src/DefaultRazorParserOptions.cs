// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptions : RazorParserOptions
    {
        public DefaultRazorParserOptions(DirectiveDescriptor[] directives, bool designTime, bool parseLeadingDirectives, RazorLanguageVersion version)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            Directives = directives;
            DesignTime = designTime;
            ParseLeadingDirectives = parseLeadingDirectives;
            Version = version;
            FeatureFlags = RazorParserFeatureFlags.Create(Version);
        }

        public override bool DesignTime { get; }

        public override IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

        public override bool ParseLeadingDirectives { get; }

        public override RazorLanguageVersion Version { get; }

        internal override RazorParserFeatureFlags FeatureFlags { get; }
    }
}
