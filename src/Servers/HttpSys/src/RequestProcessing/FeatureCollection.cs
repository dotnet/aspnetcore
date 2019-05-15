// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal sealed class FeatureCollection<TContext> : FeatureCollection
    {
        public FeatureCollection(IFeatureCollection defaults) : base(defaults) { }
    }
}
