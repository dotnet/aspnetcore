// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorParserOptionsBuilder
    {
        public virtual RazorConfiguration Configuration => null;

        public abstract bool DesignTime { get; }

        public abstract ICollection<DirectiveDescriptor> Directives { get; }

        public abstract bool ParseLeadingDirectives { get; set; }

        public virtual RazorLanguageVersion LanguageVersion { get; }

        public abstract RazorParserOptions Build();

        public virtual void SetDesignTime(bool designTime)
        {
        }
    }
}
