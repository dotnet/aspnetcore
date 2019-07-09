// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a reference to a rendered element.
    /// </summary>
    public readonly struct ElementRef
    {
        static long _nextIdForWebAssemblyOnly = 1;

        /// <summary>
        /// Gets a unique identifier for <see cref="ElementRef" />.
        /// </summary>
        /// <remarks>
        /// The Id is unique at least within the scope of a given user/circuit.
        /// This property is public to support Json serialization and should not be used by user code.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string __internalId { get; }

        internal string Id => __internalId;

        private ElementRef(string id)
        {
            __internalId = id;
        }

        internal static ElementRef CreateWithUniqueId()
            => new ElementRef(CreateUniqueId());

        static string CreateUniqueId()
        {
            if (PlatformInfo.IsWebAssembly)
            {
                // On WebAssembly there's only one user, so it's fine to expose the number
                // of IDs that have been assigned, and this is cheaper than creating a GUID.
                // It's unfortunate that this still involves a heap allocation. If that becomes
                // a problem we could extend RenderTreeFrame to have both "string" and "long"
                // fields for ElementRefCaptureId, of which only one would be in use depending
                // on the platform.
                var id = Interlocked.Increment(ref _nextIdForWebAssemblyOnly);
                return id.ToString();
            }
            else
            {
                // For remote rendering, it's important not to disclose any cross-user state,
                // such as the number of IDs that have been assigned.
                return Guid.NewGuid().ToString("D");
            }
        }
    }
}
