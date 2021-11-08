// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

[Obsolete("This class is obsolete and will be removed in a future version. The recommended alternative is " + nameof(RazorProjectEngineBuilder) + ".")]
public interface IRazorEngineBuilder
{
    ICollection<IRazorEngineFeature> Features { get; }

    IList<IRazorEnginePhase> Phases { get; }

    bool DesignTime { get; }

    RazorEngine Build();
}
