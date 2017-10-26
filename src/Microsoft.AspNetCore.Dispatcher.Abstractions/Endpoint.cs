// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Dispatcher
{
    [DebuggerDisplay("{GetType().Name} - '{DisplayName,nq}'")]
    public abstract class Endpoint
    {
        public abstract string DisplayName { get; }

        public abstract MetadataCollection Metadata { get; }
    }
}
