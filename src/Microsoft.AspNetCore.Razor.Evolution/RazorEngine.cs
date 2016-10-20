// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorEngine
    {
        public static RazorEngine Create()
        {
            return Create(configure: null);
        }

        public static RazorEngine Create(Action<IRazorEngineBuilder> configure)
        {
            var builder = new DefaultRazorEngineBuilder();
            configure?.Invoke(builder);
            return builder.Build();
        }

        public abstract IReadOnlyList<IRazorEngineFeature> Features { get; }

        public abstract IReadOnlyList<IRazorEnginePhase> Phases { get; }

        public abstract void Process(RazorCodeDocument document);
    }
}
