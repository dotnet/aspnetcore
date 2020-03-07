// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a reference to a rendered element.
    /// </summary>
    public readonly struct ElementReference
    {
        private static long _nextIdForWebAssemblyOnly = 1;

        /// <summary>
        /// Gets a unique identifier for <see cref="ElementReference" />.
        /// </summary>
        /// <remarks>
        /// The Id is unique at least within the scope of a given user/circuit.
        /// This property is public to support Json serialization and should not be used by user code.
        /// </remarks>
        public string Id { get; }

        public ElementReference(string id)
        {
            Id = id;
        }

        internal static ElementReference CreateWithUniqueId()
            => new ElementReference(CreateUniqueId());

        private static string CreateUniqueId()
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
                return id.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                // For remote rendering, it's important not to disclose any cross-user state,
                // such as the number of IDs that have been assigned.
                return Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            }
        }
    }
}
