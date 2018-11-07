// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Memory
{
    public class PostEvictionCallbackRegistration
    {
        public PostEvictionDelegate EvictionCallback { get; set; }

        public object State { get; set; }
    }
}
