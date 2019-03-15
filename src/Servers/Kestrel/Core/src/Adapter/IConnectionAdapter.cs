// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter
{
    public interface IConnectionAdapter
    {
        bool IsHttps { get; }
        Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context);
    }
}
