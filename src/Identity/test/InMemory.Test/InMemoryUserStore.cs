// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Test;

namespace Microsoft.AspNetCore.Identity.InMemory;

public class InMemoryUserStore<TUser> :
    IUserLoginStore<TUser>,
    IUserClaimStore<TUser>,
    IUserPasswordStore<TUser>,
    IUserSecurityStampStore<TUser>,
    IUserEmailStore<TUser>,
    IUserLockoutStore<TUser>,
    IUserPhoneNumberStore<TUser>,
    IQueryableUserStore<TUser>,
    IUserTwoFactorStore<TUser>,
    IUserAuthenticationTokenStore<TUser>,
    IUserAuthenticatorKeyStore<TUser>,
    IUserTwoFactorRecoveryCodeStore<TUser>
    where TUser : PocoUser
{
    private readonly Dictionary<string, TUser> _logins = new Dictionary<string, TUser>();

    private readonly Dictionary<string, TUser> _users = new Dictionary<string, TUser>();

    public IQueryable<TUser> Users
    {
        get { return _users.Values.AsQueryable(); }
    }

    public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        var claims = user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
        return Task.FromResult<IList<Claim>>(claims);
    }

    public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
    {
        foreach (var claim in claims)
        {
            user.Claims.Add(new PocoUserClaim { ClaimType = claim.Type, ClaimValue = claim.Value, UserId = user.Id });
        }
        return Task.FromResult(0);
    }

    public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
    {
        var matchedClaims = user.Claims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
        foreach (var matchedClaim in matchedClaims)
        {
            matchedClaim.ClaimValue = newClaim.Value;
            matchedClaim.ClaimType = newClaim.Type;
        }
        return Task.FromResult(0);
    }

    public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
    {
        foreach (var claim in claims)
        {
            var entity =
                user.Claims.FirstOrDefault(
                    uc => uc.UserId == user.Id && uc.ClaimType == claim.Type && uc.ClaimValue == claim.Value);
            if (entity != null)
            {
                user.Claims.Remove(entity);
            }
        }
        return Task.FromResult(0);
    }

    public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.Email = email;
        return Task.FromResult(0);
    }

    public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.Email);
    }

    public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.FromResult(0);
    }

    public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.EmailConfirmed = confirmed;
        return Task.FromResult(0);
    }

    public Task<TUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
    {
        return
            Task.FromResult(
                Users.FirstOrDefault(u => u.NormalizedEmail == email));
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.LockoutEnd = lockoutEnd;
        return Task.FromResult(0);
    }

    public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.AccessFailedCount = 0;
        return Task.FromResult(0);
    }

    public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.LockoutEnabled = enabled;
        return Task.FromResult(0);
    }

    private string GetLoginKey(string loginProvider, string providerKey)
    {
        return loginProvider + "|" + providerKey;
    }

    public virtual Task AddLoginAsync(TUser user, UserLoginInfo login,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        user.Logins.Add(new PocoUserLogin
        {
            UserId = user.Id,
            ProviderKey = login.ProviderKey,
            LoginProvider = login.LoginProvider,
            ProviderDisplayName = login.ProviderDisplayName
        });
        _logins[GetLoginKey(login.LoginProvider, login.ProviderKey)] = user;
        return Task.FromResult(0);
    }

    public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var loginEntity =
            user.Logins.SingleOrDefault(
                l =>
                    l.ProviderKey == providerKey && l.LoginProvider == loginProvider &&
                    l.UserId == user.Id);
        if (loginEntity != null)
        {
            user.Logins.Remove(loginEntity);
        }
        _logins[GetLoginKey(loginProvider, providerKey)] = null;
        return Task.FromResult(0);
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        IList<UserLoginInfo> result = user.Logins
            .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList();
        return Task.FromResult(result);
    }

    public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
    {
        string key = GetLoginKey(loginProvider, providerKey);
        if (_logins.ContainsKey(key))
        {
            return Task.FromResult(_logins[key]);
        }
        return Task.FromResult<TUser>(null);
    }

    public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.Id);
    }

    public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.UserName = userName;
        return Task.FromResult(0);
    }

    public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_users.ContainsKey(userId))
        {
            return Task.FromResult(_users[userId]);
        }
        return Task.FromResult<TUser>(null);
    }

    public void Dispose()
    {
    }

    public Task<TUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return
            Task.FromResult(
                Users.FirstOrDefault(u => u.NormalizedUserName == userName));
    }

    public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (user == null || !_users.ContainsKey(user.Id))
        {
            throw new InvalidOperationException("Unknown user");
        }
        _users.Remove(user.Id);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.PasswordHash = passwordHash;
        return Task.FromResult(0);
    }

    public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.PasswordHash != null);
    }

    public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.PhoneNumber = phoneNumber;
        return Task.FromResult(0);
    }

    public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.FromResult(0);
    }

    public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.SecurityStamp = stamp;
        return Task.FromResult(0);
    }

    public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.SecurityStamp);
    }

    public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.TwoFactorEnabled = enabled;
        return Task.FromResult(0);
    }

    public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        user.NormalizedUserName = userName;
        return Task.FromResult(0);
    }

    public Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
    {
        ArgumentNullException.ThrowIfNull(claim);

        var query = from user in Users
                    where user.Claims.Where(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value).FirstOrDefault() != null
                    select user;

        return Task.FromResult<IList<TUser>>(query.ToList());
    }

    public Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
    {
        var tokenEntity =
            user.Tokens.SingleOrDefault(
                l =>
                    l.TokenName == name && l.LoginProvider == loginProvider &&
                    l.UserId == user.Id);
        if (tokenEntity != null)
        {
            tokenEntity.TokenValue = value;
        }
        else
        {
            user.Tokens.Add(new PocoUserToken
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                TokenName = name,
                TokenValue = value
            });
        }
        return Task.FromResult(0);
    }

    public Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var tokenEntity =
            user.Tokens.SingleOrDefault(
                l =>
                    l.TokenName == name && l.LoginProvider == loginProvider &&
                    l.UserId == user.Id);
        if (tokenEntity != null)
        {
            user.Tokens.Remove(tokenEntity);
        }
        return Task.FromResult(0);
    }

    public Task<string> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var tokenEntity =
            user.Tokens.SingleOrDefault(
                l =>
                    l.TokenName == name && l.LoginProvider == loginProvider &&
                    l.UserId == user.Id);
        return Task.FromResult(tokenEntity?.TokenValue);
    }

    private const string AuthenticatorStoreLoginProvider = "[AspNetAuthenticatorStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";

    public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
    {
        return SetTokenAsync(user, AuthenticatorStoreLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);
    }

    public Task<string> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
    {
        return GetTokenAsync(user, AuthenticatorStoreLoginProvider, AuthenticatorKeyTokenName, cancellationToken);
    }

    public Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var mergedCodes = string.Join(";", recoveryCodes);
        return SetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
    }

    public async Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        var mergedCodes = await GetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
        var splitCodes = mergedCodes.Split(';');
        if (splitCodes.Contains(code))
        {
            var updatedCodes = new List<string>(splitCodes.Where(s => s != code));
            await ReplaceCodesAsync(user, updatedCodes, cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        var mergedCodes = await GetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
        if (mergedCodes.Length > 0)
        {
            return mergedCodes.Split(';').Length;
        }
        return 0;
    }
}
