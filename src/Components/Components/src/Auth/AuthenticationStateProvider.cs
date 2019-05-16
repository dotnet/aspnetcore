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
        /// Gets an <see cref="AuthenticationState"/> instance that describes
        /// the current user.
        /// </summary>
        /// <returns>An <see cref="AuthenticationState"/> instance that describes the current user.</returns>
        public abstract Task<AuthenticationState> GetAuthenticationStateAsync();

        /// <summary>
        /// An event that provides notification when the <see cref="AuthenticationState"/>
        /// has changed. For example, this event may be raised if a user logs in or out.
        /// </summary>
#pragma warning disable 0067 // "Never used" (it's only raised by subclasses)
        public event AuthenticationStateChangedHandler AuthenticationStateChanged;
#pragma warning restore 0067

        /// <summary>
        /// Raises the <see cref="AuthenticationStateChanged"/> event.
        /// </summary>
        /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="AuthenticationState"/>.</param>
        protected void NotifyAuthenticationStateChanged(Task<AuthenticationState> task)
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
    /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="AuthenticationState"/>.</param>
    public delegate void AuthenticationStateChangedHandler(Task<AuthenticationState> task);
}
