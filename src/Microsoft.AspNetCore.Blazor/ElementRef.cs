// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Internal;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNetCore.Blazor
{
    /// <summary>
    /// Represents a reference to a rendered element.
    /// </summary>
    public readonly struct ElementRef : ICustomJsonSerializer
    {
        // Static to ensure uniqueness even if there are multiple Renderer instances
        // This would not be necessary if the JS-side code maintained a lookup from capureId to Element instances,
        // but we're not doing that presently as it causes more work during disposal to remove those entries
        // WARNING: Once we support server-side rendering, we should check if running on the server and avoid
        //          populating element reference capture IDs at all, because doing so could (a) eventually
        //          overflow the static int, and (b) disclose information to clients about how many other
        //          requests the server is handling, etc. In general, as part of implementing SSR, we need to
        //          audit the code it calls for any use of statics.
        private static int _nextId = 0;

        internal int Id { get; }

        private ElementRef(int id)
        {
            Id = id;
        }

        internal static ElementRef CreateWithUniqueId()
            => new ElementRef(Interlocked.Increment(ref _nextId));

        object ICustomJsonSerializer.ToJsonPrimitive()
        {
            return new Dictionary<string, object>
            {
                { "_blazorElementRef", Id }
            };
        }
    }
}
