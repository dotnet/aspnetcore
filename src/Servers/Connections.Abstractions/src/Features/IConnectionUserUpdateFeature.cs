// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that notifies subscribers when the user associated with the connection is updated,
/// for example via an authentication refresh.
/// </summary>
public interface IConnectionUserUpdateFeature
{
    /// <summary>
    /// Raised after the <see cref="IConnectionUserFeature.User"/> has been updated.
    /// The first argument is the previous principal (may be <c>null</c> if no user was previously set),
    /// and the second argument is the new principal.
    /// </summary>
    /// <remarks>
    /// Handlers should be quick and avoid blocking; the event is raised on the thread that performed the update.
    /// Exceptions thrown from handlers are not propagated to the caller of the update.
    /// </remarks>
    event Action<ClaimsPrincipal?, ClaimsPrincipal> UserUpdated;
}
