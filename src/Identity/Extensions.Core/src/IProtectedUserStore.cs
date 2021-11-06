// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Marker interface used to signal that the store supports the <see cref="StoreOptions.ProtectPersonalData"/> flag.
/// </summary>
/// <typeparam name="TUser">The type that represents a user.</typeparam>
public interface IProtectedUserStore<TUser> : IUserStore<TUser> where TUser : class
{ }
