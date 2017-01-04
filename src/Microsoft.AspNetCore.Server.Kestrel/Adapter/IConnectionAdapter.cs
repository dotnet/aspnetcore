// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Adapter
{
    public interface IConnectionAdapter
    {
        Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context);
    }
}
