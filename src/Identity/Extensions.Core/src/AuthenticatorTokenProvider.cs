// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used for authenticator code verification.
/// </summary>
public class AuthenticatorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class
{
    /// <summary>
    /// Checks if a two-factor authentication token can be generated for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
    /// <param name="user">The <typeparamref name="TUser"/> to check for the possibility of generating a two-factor authentication token.</param>
    /// <returns>True if the user has an authenticator key set, otherwise false.</returns>
    public virtual async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        var key = await manager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>
    /// Returns an empty string since no authenticator codes are sent.
    /// </summary>
    /// <param name="purpose">Ignored.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
    /// <param name="user">The <typeparamref name="TUser"/>.</param>
    /// <returns>string.Empty.</returns>
    public virtual Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="purpose"></param>
    /// <param name="token"></param>
    /// <param name="manager"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public virtual async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        var key = await manager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        int code;
        if (key == null || !int.TryParse(token, out code))
        {
            return false;
        }

        var keyBytes = Base32.FromBase32(key);

#if NET6_0_OR_GREATER
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#else
        using var hash = new HMACSHA1(keyBytes);
        var unixTimestamp = Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds));
#endif

        var timestep = Convert.ToInt64(unixTimestamp / 30);
        // Allow codes from 90s in each direction (we could make this configurable?)
        for (int i = -2; i <= 2; i++)
        {
#if NET6_0_OR_GREATER
            var expectedCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)(timestep + i), modifierBytes: null);
#else
            var expectedCode = Rfc6238AuthenticationService.ComputeTotp(hash, (ulong)(timestep + i), modifierBytes: null);
#endif
            if (expectedCode == code)
            {
                return true;
            }
        }

        return false;
    }
}
