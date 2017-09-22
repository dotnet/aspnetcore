// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherValueAddress : Address
    {
        public DispatcherValueAddress(object values)
            : this(values, Array.Empty<object>(), null)
        {
        }


        public DispatcherValueAddress(object values, IEnumerable<object> metadata)
            : this(values, metadata, null)
        {
        }

        public DispatcherValueAddress(object values, IEnumerable<object> metadata, string displayName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Values = new DispatcherValueCollection(values);
            Metadata = metadata.ToArray();
            DisplayName = displayName;
        }

        public override string DisplayName { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public DispatcherValueCollection Values { get; }
    }
}
