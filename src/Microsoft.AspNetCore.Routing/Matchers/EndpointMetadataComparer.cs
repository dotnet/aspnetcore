// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract class EndpointMetadataComparer<TMetadata> : IComparer<Endpoint> where TMetadata : class
    {
        public static readonly EndpointMetadataComparer<TMetadata> Default = new DefaultComparer<TMetadata>();

        public int Compare(Endpoint x, Endpoint y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            return CompareMetadata(GetMetadata(x), GetMetadata(y));
        }

        protected virtual TMetadata GetMetadata(Endpoint endpoint)
        {
            return endpoint.Metadata.GetMetadata<TMetadata>();
        }

        protected virtual int CompareMetadata(TMetadata x, TMetadata y)
        {
            // The default policy is that if x endpoint defines TMetadata, and
            // y endpoint does not, then x is *more specific* than y. We return
            // -1 for this case so that x will come first in the sort order.

            if (x == null && y != null)
            {
                // y is more specific
                return 1;
            }
            else if (x != null && y == null)
            {
                // x is more specific
                return -1;
            }

            // both endpoints have this metadata, or both do not have it, they have
            // the same specificity.
            return 0;
        }

        private class DefaultComparer<T> : EndpointMetadataComparer<T> where T : class
        {
        }
    }
}
