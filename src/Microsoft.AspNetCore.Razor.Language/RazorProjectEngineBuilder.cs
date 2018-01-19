// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectEngineBuilder
    {
        public abstract RazorProjectFileSystem FileSystem { get; }

        public abstract ICollection<IRazorFeature> Features { get; }

        public abstract IList<IRazorEnginePhase> Phases { get; }

        public abstract bool DesignTime { get; }

        public abstract RazorProjectEngine Build();
    }
}
