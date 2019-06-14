// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionBuilder
    {
        IServiceProvider ApplicationServices { get; }

        IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

        ConnectionDelegate Build();
    }
}
