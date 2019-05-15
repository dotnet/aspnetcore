// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides information about the authentication state of the current user.
    /// </summary>
    public abstract class AuthenticationStateProvider
    {
        /// <summary>
        /// Gets an <see cref="IAuthenticationState"/> instance that describes
        /// the current user.
        /// </summary>
        /// <param name="forceRefresh">If true, instructs the provider to re-determine the user's authentication state, which can be an expensive operation. If false, the provider may reuse data cached for the current user.</param>
        /// <returns>An <see cref="IAuthenticationState"/> instance that describes the current user.</returns>
        public abstract Task<IAuthenticationState> GetAuthenticationStateAsync(bool forceRefresh);

        /// <summary>
        /// An event that provides notification when the <see cref="IAuthenticationState"/>
        /// has changed. For example, this event may be raised if a user logs in or out.
        /// </summary>
#pragma warning disable 0067 // "Never used" (it's only raised by subclasses)
        public event AuthenticationStateChangedHandler AuthenticationStateChanged;
#pragma warning restore 0067

        /// <summary>
        /// Raises the <see cref="AuthenticationStateChanged"/> event.
        /// </summary>
        /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="IAuthenticationState"/>.</param>
        protected void NotifyAuthenticationStateChanged(Task<IAuthenticationState> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            AuthenticationStateChanged?.Invoke(task);
        }
    }

    /// <summary>
    /// A handler for the <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/> event.
    /// </summary>
    /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="IAuthenticationState"/>.</param>
    public delegate void AuthenticationStateChangedHandler(Task<IAuthenticationState> task);
}
