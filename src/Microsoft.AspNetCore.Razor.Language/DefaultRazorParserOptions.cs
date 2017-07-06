// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptions : RazorParserOptions
    {
        public DefaultRazorParserOptions(DirectiveDescriptor[] directives, bool designTime, bool parseLeadingDirectives)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            Directives = directives;
            DesignTime = designTime;
            ParseLeadingDirectives = parseLeadingDirectives;
        }

        public override bool DesignTime { get; }

        public override IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

        public override bool ParseLeadingDirectives { get; }
    }
}
