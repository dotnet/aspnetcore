// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class EndpointDataSource
    {
        public virtual IChangeToken ChangeToken { get; }

        // Plan is to replace ChangeToken property with GetChangeToken
        // Temporarily have both to avoid breaking MVC
        // https://github.com/aspnet/Routing/issues/634
        public virtual IChangeToken GetChangeToken()
        {
            return ChangeToken;
        }

        public abstract IReadOnlyList<Endpoint> Endpoints { get; }
    }
}
