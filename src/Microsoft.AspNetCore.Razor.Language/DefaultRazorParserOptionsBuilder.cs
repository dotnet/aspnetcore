// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptionsBuilder : RazorParserOptionsBuilder
    {
        public DefaultRazorParserOptionsBuilder(bool designTime)
        {
            DesignTime = designTime;
        }

        public override bool DesignTime { get; }

        public override ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

        public override bool ParseLeadingDirectives { get; set; }

        public override RazorParserOptions Build()
        {
            return new DefaultRazorParserOptions(Directives.ToArray(), DesignTime, ParseLeadingDirectives);
        }
    }
}
