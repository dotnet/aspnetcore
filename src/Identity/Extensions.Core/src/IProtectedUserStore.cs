// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Marker interface used to signal that the store supports the <see cref="StoreOptions.ProtectPersonalData"/> flag.
    /// </summary>
    /// <typeparam name="TUser">The type that represents a user.</typeparam>
    public interface IProtectedUserStore<TUser> : IUserStore<TUser> where TUser : class
    { }
}