// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorParserOptionsBuilder
    {
        public abstract bool DesignTime { get; }

        public abstract ICollection<DirectiveDescriptor> Directives { get; }

        public abstract bool ParseLeadingDirectives { get; set; }

        public abstract RazorParserOptions Build();
    }
}
