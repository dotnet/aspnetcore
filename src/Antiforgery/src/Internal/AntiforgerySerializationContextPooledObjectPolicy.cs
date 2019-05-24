// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery
{
    internal class AntiforgerySerializationContextPooledObjectPolicy : IPooledObjectPolicy<AntiforgerySerializationContext>
    {
        public AntiforgerySerializationContext Create()
        {
            return new AntiforgerySerializationContext();
        }

        public bool Return(AntiforgerySerializationContext obj)
        {
            obj.Reset();

            return true;
        }
    }
}
