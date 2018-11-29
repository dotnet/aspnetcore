// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptionsBuilder : RazorParserOptionsBuilder
    {
        private bool _designTime;

        public DefaultRazorParserOptionsBuilder(RazorConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Configuration = configuration;
            LanguageVersion = configuration.LanguageVersion;
        }

        public DefaultRazorParserOptionsBuilder(bool designTime, RazorLanguageVersion version)
        {
            _designTime = designTime;
            LanguageVersion = version;
        }

        public override RazorConfiguration Configuration { get; }

        public override bool DesignTime => _designTime;

        public override ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

        public override bool ParseLeadingDirectives { get; set; }

        public override RazorLanguageVersion LanguageVersion { get; }

        public override RazorParserOptions Build()
        {
            return new DefaultRazorParserOptions(Directives.ToArray(), DesignTime, ParseLeadingDirectives, LanguageVersion);
        }

        public override void SetDesignTime(bool designTime)
        {
            _designTime = designTime;
        }
    }
}
