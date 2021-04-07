// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// An interface for an <see cref="IRouter"/> with a name.
    /// </summary>
    public interface INamedRouter : IRouter
    {
        /// <summary>
        /// The name of the router. Can be null.
        /// </summary>
        string? Name { get; }
    }
}
