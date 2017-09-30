// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class EndpointSelector
    {
        public abstract Task SelectAsync(EndpointSelectorContext context);

        public virtual void Initialize(IEndpointCollectionProvider endpointProvider)
        {
        }
    }
}
