// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ConsumesMetadata : IConsumesMetadata
    {
        public ConsumesMetadata(string[] contentTypes)
        {
            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ContentTypes = contentTypes;
        }

        public IReadOnlyList<string> ContentTypes { get; }
    }
}
