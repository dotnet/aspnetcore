// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
/// Test user class
/// </summary>
public class PocoUser : PocoUser<string>
{
    /// <summary>
    /// Ctor
    /// </summary>
    public PocoUser()
    {
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="userName"></param>
    public PocoUser(string userName) : this()
    {
        UserName = userName;
    }
}

/// <summary>
/// Test user
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class PocoUser<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// ctor
    /// </summary>
    public PocoUser() { }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="userName"></param>
    public PocoUser(string userName) : this()
    {
        UserName = userName;
    }

    /// <summary>
    /// Id
    /// </summary>
    [PersonalData]
    public virtual TKey Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [PersonalData]
    public virtual string UserName { get; set; }

    /// <summary>
    /// normalized user name
    /// </summary>
    public virtual string NormalizedUserName { get; set; }

    /// <summary>
    ///     Email
    /// </summary>
    [PersonalData]
    public virtual string Email { get; set; }

    /// <summary>
    /// normalized email
    /// </summary>
    public virtual string NormalizedEmail { get; set; }

    /// <summary>
    ///     True if the email is confirmed, default is false
    /// </summary>
    [PersonalData]
    public virtual bool EmailConfirmed { get; set; }

    /// <summary>
    ///     The salted/hashed form of the user password
    /// </summary>
    public virtual string PasswordHash { get; set; }

    /// <summary>
    /// A random value that should change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public virtual string SecurityStamp { get; set; }

    /// <summary>
    /// A random value that should change whenever a user is persisted to the store
    /// </summary>
    public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     PhoneNumber for the user
    /// </summary>
    [PersonalData]
    public virtual string PhoneNumber { get; set; }

    /// <summary>
    ///     True if the phone number is confirmed, default is false
    /// </summary>
    [PersonalData]
    public virtual bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    ///     Is two factor enabled for the user
    /// </summary>
    [PersonalData]
    public virtual bool TwoFactorEnabled { get; set; }

    /// <summary>
    ///     DateTime in UTC when lockout ends, any time in the past is considered not locked out.
    /// </summary>
    public virtual DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    ///     Is lockout enabled for this user
    /// </summary>
    public virtual bool LockoutEnabled { get; set; }

    /// <summary>
    ///     Used to record failures for the purposes of lockout
    /// </summary>
    public virtual int AccessFailedCount { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual ICollection<PocoUserRole<TKey>> Roles { get; private set; } = new List<PocoUserRole<TKey>>();
    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual ICollection<PocoUserClaim<TKey>> Claims { get; private set; } = new List<PocoUserClaim<TKey>>();
    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual ICollection<PocoUserLogin<TKey>> Logins { get; private set; } = new List<PocoUserLogin<TKey>>();
    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual ICollection<PocoUserToken<TKey>> Tokens { get; private set; } = new List<PocoUserToken<TKey>>();
}
