// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorParserOptionsBuilder
{
    public virtual RazorConfiguration Configuration => null;

    public abstract bool DesignTime { get; }

    public abstract ICollection<DirectiveDescriptor> Directives { get; }

    public virtual string FileKind => null;

    public abstract bool ParseLeadingDirectives { get; set; }

    public virtual RazorLanguageVersion LanguageVersion { get; }

    public abstract RazorParserOptions Build();

    public virtual void SetDesignTime(bool designTime)
    {
    }
}
