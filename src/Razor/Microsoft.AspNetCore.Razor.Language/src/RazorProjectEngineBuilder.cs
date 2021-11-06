// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectEngineBuilder
{
    public abstract RazorConfiguration Configuration { get; }

    public abstract RazorProjectFileSystem FileSystem { get; }

    public abstract ICollection<IRazorFeature> Features { get; }

    public abstract IList<IRazorEnginePhase> Phases { get; }

    public abstract RazorProjectEngine Build();
}
