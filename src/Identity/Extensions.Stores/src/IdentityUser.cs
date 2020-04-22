// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// The default implementation of <see cref="IdentityUser{TKey}"/> which uses a string as a primary key.
    /// </summary>
    public class IdentityUser : IdentityUser<string>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to form a new GUID string value.
        /// </remarks>
        public IdentityUser()
        {
            Id = Guid.NewGuid().ToString();
            SecurityStamp = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <remarks>
        /// The Id property is initialized to form a new GUID string value.
        /// </remarks>
        public IdentityUser(string userName) : this()
        {
            UserName = userName;
        }
    }

    /// <summary>
    /// Represents a user in the identity system
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    public class IdentityUser<TKey> : IIdentityUser<TKey> where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
        /// </summary>
        public IdentityUser() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
        /// </summary>
        /// <param name="userName">The user name.</param>
        public IdentityUser(string userName) : this()
        {
            UserName = userName;
        }

        /// <inheritdoc/>
        [PersonalData]
        public virtual TKey Id { get; set; }

        /// <inheritdoc/>
        [ProtectedPersonalData]
        public virtual string UserName { get; set; }

        /// <inheritdoc/>
        public virtual string NormalizedUserName { get; set; }

        /// <inheritdoc/>
        [ProtectedPersonalData]
        public virtual string Email { get; set; }

        /// <inheritdoc/>
        public virtual string NormalizedEmail { get; set; }

        /// <inheritdoc/>
        [PersonalData]
        public virtual bool EmailConfirmed { get; set; }

        /// <inheritdoc/>
        public virtual string PasswordHash { get; set; }

        /// <inheritdoc/>
        public virtual string SecurityStamp { get; set; }

        /// <inheritdoc/>
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        /// <inheritdoc/>
        [ProtectedPersonalData]
        public virtual string PhoneNumber { get; set; }

        /// <inheritdoc/>
        [PersonalData]
        public virtual bool PhoneNumberConfirmed { get; set; }

        /// <inheritdoc/>
        [PersonalData]
        public virtual bool TwoFactorEnabled { get; set; }

        /// <inheritdoc/>
        public virtual DateTimeOffset? LockoutEnd { get; set; }

        /// <inheritdoc/>
        public virtual bool LockoutEnabled { get; set; }

        /// <inheritdoc/>
        public virtual int AccessFailedCount { get; set; }

        /// <summary>
        /// Returns the username for this user.
        /// </summary>
        public override string ToString()
            => UserName;
    }
}
