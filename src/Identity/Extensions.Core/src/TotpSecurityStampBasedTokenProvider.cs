// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a token provider that generates time-based codes using the user's security stamp.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public abstract class TotpSecurityStampBasedTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : class
{
    /// <summary>
    /// Generates a token for the specified <paramref name="user"/> and <paramref name="purpose"/>.
    /// </summary>
    /// <param name="purpose">The purpose the token will be used for.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user a token should be generated for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the token for the specified 
    /// <paramref name="user"/> and <paramref name="purpose"/>.
    /// </returns>
    /// <remarks>
    /// The <paramref name="purpose"/> parameter allows a token generator to be used for multiple types of token whilst
    /// insuring a token for one purpose cannot be used for another. For example if you specified a purpose of "Email" 
    /// and validated it with the same purpose a token with the purpose of TOTP would not pass the check even if it was
    /// for the same user.
    /// 
    /// Implementations of <see cref="IUserTwoFactorTokenProvider{TUser}"/> should validate that purpose is not null or empty to
    /// help with token separation.
    /// </remarks>
    public virtual async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullThrowHelper.ThrowIfNull(manager);
        var token = await manager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);

        return Rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns a flag indicating whether the specified <paramref name="token"/> is valid for the given
    /// <paramref name="user"/> and <paramref name="purpose"/>.
    /// </summary>
    /// <param name="purpose">The purpose the token will be used for.</param>
    /// <param name="token">The token to validate.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user a token should be validated for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the a flag indicating the result
    /// of validating the <paramref name="token"> for the specified </paramref><paramref name="user"/> and <paramref name="purpose"/>.
    /// The task will return true if the token is valid, otherwise false.
    /// </returns>
    public virtual async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullThrowHelper.ThrowIfNull(manager);
        int code;
        if (!int.TryParse(token, out code))
        {
            return false;
        }
        var securityToken = await manager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);

        return securityToken != null && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }

    /// <summary>
    /// Returns a constant, provider and user unique modifier used for entropy in generated tokens from user information.
    /// </summary>
    /// <param name="purpose">The purpose the token will be generated for.</param>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user a token should be generated for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing a constant modifier for the specified 
    /// <paramref name="user"/> and <paramref name="purpose"/>.
    /// </returns>
    public virtual async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullThrowHelper.ThrowIfNull(manager);
        var userId = await manager.GetUserIdAsync(user).ConfigureAwait(false);

        return $"Totp:{purpose}:{userId}";
    }

    /// <summary>
    /// Returns a flag indicating whether the token provider can generate a token suitable for two-factor authentication token for
    /// the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user a token could be generated for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the a flag indicating if a two
    /// factor token could be generated by this provider for the specified <paramref name="user"/>.
    /// The task will return true if a two-factor authentication token could be generated, otherwise false.
    /// </returns>
    public abstract Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
}
