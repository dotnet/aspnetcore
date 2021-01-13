// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal
{
    /// <summary>
    /// This is an internal API that supports the Microsoft.AspNetCore.Components.WebAssembly.Authentication
    /// infrastructure and not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    public interface IAccessTokenProviderAccessor
    {
        /// <summary>
        /// This is an internal API that supports the Microsoft.AspNetCore.Components.WebAssembly.Authentication
        /// infrastructure and not subject to the same compatibility standards as public APIs.
        /// It may be changed or removed without notice in any release.
        /// </summary>
        IAccessTokenProvider TokenProvider { get; }
    }
}
