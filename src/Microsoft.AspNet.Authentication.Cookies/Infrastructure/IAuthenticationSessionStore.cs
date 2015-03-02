// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.Cookies.Infrastructure
{
    /// <summary>
    /// This provides an abstract storage mechanic to preserve identity information on the server
    /// while only sending a simple identifier key to the client. This is most commonly used to mitigate
    /// issues with serializing large identities into cookies.
    /// </summary>
    public interface IAuthenticationSessionStore
    {
        /// <summary>
        /// Store the identity ticket and return the associated key.
        /// </summary>
        /// <param name="ticket">The identity information to store.</param>
        /// <returns>The key that can be used to retrieve the identity later.</returns>
        Task<string> StoreAsync(AuthenticationTicket ticket);

        /// <summary>
        /// Tells the store that the given identity should be updated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        Task RenewAsync(string key, AuthenticationTicket ticket);

        /// <summary>
        /// Retrieves an identity from the store for the given key.
        /// </summary>
        /// <param name="key">The key associated with the identity.</param>
        /// <returns>The identity associated with the given key, or if not found.</returns>
        Task<AuthenticationTicket> RetrieveAsync(string key);

        /// <summary>
        /// Remove the identity associated with the given key.
        /// </summary>
        /// <param name="key">The key associated with the identity.</param>
        /// <returns></returns>
        Task RemoveAsync(string key);
    }
}
