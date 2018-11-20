// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to determine if a handler wants to participate in request processing.
    /// </summary>
    public interface IAuthenticationRequestHandler : IAuthenticationHandler
    {
        /// <summary>
        /// Returns true if request processing should stop.
        /// </summary>
        /// <returns></returns>
        Task<bool> HandleRequestAsync();
    }

}
