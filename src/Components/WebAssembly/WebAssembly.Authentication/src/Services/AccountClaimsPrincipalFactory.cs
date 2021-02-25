// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Converts <see cref="RemoteUserAccount" /> into a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <typeparam name="TAccount">The account type.</typeparam>
    public class AccountClaimsPrincipalFactory<TAccount> where TAccount : RemoteUserAccount
    {
        private readonly IAccessTokenProviderAccessor _accessor;

#pragma warning disable PUB0001 // Pubternal type in public API
        /// <summary>
        /// Initialize a new instance of <see cref="AccountClaimsPrincipalFactory{TAccount}"/>.
        /// </summary>
        /// <param name="accessor"></param>
        public AccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) => _accessor = accessor;

        /// <summary>
        /// Gets or sets the <see cref="IAccessTokenProvider"/>.
        /// </summary>
        public IAccessTokenProvider TokenProvider => _accessor.TokenProvider;

        /// <summary>
        /// Converts the <paramref name="account"/> into the final <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="options">The <see cref="RemoteAuthenticationUserOptions"/> to configure the <see cref="ClaimsPrincipal"/> with.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/>that will contain the <see cref="ClaimsPrincipal"/> user when completed.</returns>
        public virtual ValueTask<ClaimsPrincipal> CreateUserAsync(
            TAccount account,
            RemoteAuthenticationUserOptions options)
        {
            var identity = account != null ? new ClaimsIdentity(
            options.AuthenticationType,
            options.NameClaim,
            options.RoleClaim) : new ClaimsIdentity();

            if (account != null)
            {
                foreach (var kvp in account.AdditionalProperties)
                {
                    var name = kvp.Key;
                    var value = kvp.Value;
                    if (value != null ||
                        (value is JsonElement element && element.ValueKind != JsonValueKind.Undefined && element.ValueKind != JsonValueKind.Null))
                    {
                        identity.AddClaim(new Claim(name, value.ToString()));
                    }
                }
            }

            return new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(identity));
        }
    }
}
