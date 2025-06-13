// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;

namespace IdentitySample.PasskeyUI;

public sealed class InMemoryUserStore<TUser> :
    IQueryableUserStore<TUser>,
    IUserPasskeyStore<TUser>
    where TUser : PocoUser
{
    private readonly Dictionary<string, TUser> _users = [];

    public IQueryable<TUser> Users => _users.Values.AsQueryable();

    public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
    {
        if (!_users.Remove(user.Id))
        {
            throw new InvalidOperationException($"Unknown user with ID '{user.Id}'.");
        }
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(_users.TryGetValue(userId, out var result) ? result : null);

    public Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult(_users.Values.FirstOrDefault(u => string.Equals(u.NormalizedUserName, normalizedUserName, StringComparison.Ordinal)));

    public Task<TUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
        => Task.FromResult(_users.Values.FirstOrDefault(u => u.Passkeys.Any(p => p.CredentialId.SequenceEqual(credentialId))));

    public Task<UserPasskeyInfo?> FindPasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken)
        => Task.FromResult(ToUserPasskeyInfo(user.Passkeys.FirstOrDefault(p => p.CredentialId.SequenceEqual(credentialId))));

    public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.UserName);

    public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task SetPasskeyAsync(TUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
    {
        var passkeyEntity = user.Passkeys.FirstOrDefault(p => p.CredentialId.SequenceEqual(passkey.CredentialId));
        if (passkeyEntity is null)
        {
            user.Passkeys.Add(ToPocoUserPasskey(user, passkey));
        }
        else
        {
            passkeyEntity.Name = passkey.Name;
            passkeyEntity.SignCount = passkey.SignCount;
            passkeyEntity.IsBackedUp = passkey.IsBackedUp;
            passkeyEntity.IsUserVerified = passkey.IsUserVerified;
        }
        return Task.CompletedTask;
    }

    public Task<IList<UserPasskeyInfo>> GetPasskeysAsync(TUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<UserPasskeyInfo>>(user.Passkeys.Select(ToUserPasskeyInfo).ToList()!);

    public Task RemovePasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        var passkey = user.Passkeys.SingleOrDefault(p => p.CredentialId.SequenceEqual(credentialId));
        if (passkey is not null)
        {
            user.Passkeys.Remove(passkey);
        }

        return Task.CompletedTask;
    }

    [return: NotNullIfNotNull(nameof(p))]
    private static UserPasskeyInfo? ToUserPasskeyInfo(PocoUserPasskey<string>? p)
        => p is null ? null : new(
            p.CredentialId,
            p.PublicKey,
            p.Name,
            p.CreatedAt,
            p.SignCount,
            p.Transports,
            p.IsUserVerified,
            p.IsBackupEligible,
            p.IsBackedUp,
            p.AttestationObject,
            p.ClientDataJson);

    [return: NotNullIfNotNull(nameof(p))]
    private static PocoUserPasskey<string>? ToPocoUserPasskey(TUser user, UserPasskeyInfo? p)
        => p is null ? null : new PocoUserPasskey<string>
        {
            UserId = user.Id,
            CredentialId = p.CredentialId,
            PublicKey = p.PublicKey,
            Name = p.Name,
            CreatedAt = p.CreatedAt,
            Transports = p.Transports,
            SignCount = p.SignCount,
            IsUserVerified = p.IsUserVerified,
            IsBackupEligible = p.IsBackupEligible,
            IsBackedUp = p.IsBackedUp,
            AttestationObject = p.AttestationObject,
            ClientDataJson = p.ClientDataJson,
        };

    public void Dispose()
    {
    }
}
