// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class DefaultRazorEngineBuilder : IRazorEngineBuilder
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public DefaultRazorEngineBuilder(bool designTime)
        {
            DesignTime = designTime;
            Features = new List<IRazorEngineFeature>();
            Phases = new List<IRazorEnginePhase>();
        }

        public ICollection<IRazorEngineFeature> Features { get; }

        public IList<IRazorEnginePhase> Phases { get; }

        public bool DesignTime { get; }

        public RazorEngine Build()
        {
            var features = new IRazorEngineFeature[Features.Count];
            Features.CopyTo(features, arrayIndex: 0);

            var phases = new IRazorEnginePhase[Phases.Count];
            Phases.CopyTo(phases, arrayIndex: 0);

            return new DefaultRazorEngine(features, phases);
        }
    }
}
