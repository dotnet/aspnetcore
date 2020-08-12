// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// A function that can process a connection.
    /// </summary>
    /// <param name="connection">A <see cref="MultiplexedConnectionContext" /> representing the connection.</param>
    /// <returns>A <see cref="Task"/> that represents the connection lifetime. When the task completes, the connection will be closed.</returns>
    public delegate Task MultiplexedConnectionDelegate(MultiplexedConnectionContext connection);
}
