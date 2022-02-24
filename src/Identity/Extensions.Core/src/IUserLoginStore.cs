// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for storing information that maps external login information provided
/// by Microsoft Account, Facebook etc. to a user account.
/// </summary>
/// <typeparam name="TUser">The type that represents a user.</typeparam>
public interface IUserLoginStore<TUser> : IUserStore<TUser> where TUser : class
{
    /// <summary>
    /// Adds an external <see cref="UserLoginInfo"/> to the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to add the login to.</param>
    /// <param name="login">The external <see cref="UserLoginInfo"/> to add to the specified <paramref name="user"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to remove the provided login information from the specified <paramref name="user"/>.
    /// and returns a flag indicating whether the removal succeed or not.
    /// </summary>
    /// <param name="user">The user to remove the login information from.</param>
    /// <param name="loginProvider">The login provide whose information should be removed.</param>
    /// <param name="providerKey">The key given by the external login provider for the specified user.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the associated logins for the specified <param ref="user"/>.
    /// </summary>
    /// <param name="user">The user whose associated logins to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="Task"/> for the asynchronous operation, containing a list of <see cref="UserLoginInfo"/> for the specified <paramref name="user"/>, if any.
    /// </returns>
    Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the user associated with the specified login provider and login provider key.
    /// </summary>
    /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey"/>.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="Task"/> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
    /// </returns>
    Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);
}
