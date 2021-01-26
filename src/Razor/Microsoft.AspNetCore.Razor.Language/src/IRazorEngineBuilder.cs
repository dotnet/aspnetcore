// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    [Obsolete("This class is obsolete and will be removed in a future version. The recommended alternative is " + nameof(RazorProjectEngineBuilder) + ".")]
    public interface IRazorEngineBuilder
    {
        ICollection<IRazorEngineFeature> Features { get; }

        IList<IRazorEnginePhase> Phases { get; }

        bool DesignTime { get; }

        RazorEngine Build();
    }
}
