// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubClients<T>
    {
        T All { get; }

        T AllExcept(IReadOnlyList<string> excludedIds);

        T Client(string connectionId);

        T Group(string groupName);

        T User(string userId);
    }
}
