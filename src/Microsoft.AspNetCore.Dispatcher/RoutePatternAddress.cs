// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RoutePatternAddress : Address, IRoutePatternAddress
    {
        public RoutePatternAddress(string pattern, object values, params object[] metadata)
            : this(pattern, values, null, metadata)
        {
        }

        public RoutePatternAddress(string pattern, object values, string displayName, params object[] metadata)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Pattern = pattern;
            Defaults = new DispatcherValueCollection(values);
            DisplayName = displayName;
            Metadata = new MetadataCollection(metadata);
        }

        public override string DisplayName { get; }

        public override MetadataCollection Metadata { get; }

        public string Pattern { get; }

        public DispatcherValueCollection Defaults { get; }
    }
}
