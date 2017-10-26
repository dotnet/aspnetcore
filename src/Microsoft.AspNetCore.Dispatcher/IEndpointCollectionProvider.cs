// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public interface IEndpointCollectionProvider
    {
        IReadOnlyList<Endpoint> Endpoints { get; }

        IChangeToken ChangeToken { get; }
    }
}
