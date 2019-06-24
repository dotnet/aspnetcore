// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// A function that can process an connection.
    /// </summary>
    /// <param name="connection">The new <see cref="ConnectionContext"/></param>
    /// <returns>A <see cref="Task"/> that represents the connection lifetime. When the task completes, the connection is complete.</returns>
    public delegate Task ConnectionDelegate(ConnectionContext connection);
}
