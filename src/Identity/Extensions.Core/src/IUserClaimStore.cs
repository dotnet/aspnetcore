// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a store of claims for a user.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public interface IUserClaimStore<TUser> : IUserStore<TUser> where TUser : class
{
    /// <summary>
    /// Gets a list of <see cref="Claim"/>s to be belonging to the specified <paramref name="user"/> as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose claims to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <see cref="Claim"/>s.
    /// </returns>
    Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Add claims to a user as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user to add the claim to.</param>
    /// <param name="claims">The collection of <see cref="Claim"/>s to add.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the given <paramref name="claim"/> on the specified <paramref name="user"/> with the <paramref name="newClaim"/>
    /// </summary>
    /// <param name="user">The user to replace the claim on.</param>
    /// <param name="claim">The claim to replace.</param>
    /// <param name="newClaim">The new claim to replace the existing <paramref name="claim"/> with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the specified <paramref name="claims"/> from the given <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to remove the specified <paramref name="claims"/> from.</param>
    /// <param name="claims">A collection of <see cref="Claim"/>s to remove.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of users who contain the specified <see cref="Claim"/>.
    /// </summary>
    /// <param name="claim">The claim to look for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <typeparamref name="TUser"/> who
    /// contain the specified claim.
    /// </returns>
    Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken);
}
