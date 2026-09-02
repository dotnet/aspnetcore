// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class HostInitializerCollection
{
    public HostInitializerCollection(IEnumerable<IHostInitializer> initializers)
    {
        Initializers = initializers
            .OrderBy(initializer => initializer.Order)
            .ToImmutableArray();
    }

    public ImmutableArray<IHostInitializer> Initializers { get; }
}
