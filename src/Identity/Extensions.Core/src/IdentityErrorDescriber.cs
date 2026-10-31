// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Identity.Core;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Service to enable localization for application facing identity errors.
/// </summary>
/// <remarks>
/// These errors are returned to controllers and are generally used as display messages to end users.
/// </remarks>
public class IdentityErrorDescriber
{
    /// <summary>
    /// Returns the default <see cref="IdentityError"/>.
    /// </summary>
    /// <returns>The default <see cref="IdentityError"/>.</returns>
    public virtual IdentityError DefaultError()
    {
        return new IdentityError
        {
            Code = nameof(DefaultError),
            Description = Resources.DefaultError
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a concurrency failure.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a concurrency failure.</returns>
    public virtual IdentityError ConcurrencyFailure()
    {
        return new IdentityError
        {
            Code = nameof(ConcurrencyFailure),
            Description = Resources.ConcurrencyFailure
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password mismatch.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a password mismatch.</returns>
    public virtual IdentityError PasswordMismatch()
    {
        return new IdentityError
        {
            Code = nameof(PasswordMismatch),
            Description = Resources.PasswordMismatch
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating an invalid token.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating an invalid token.</returns>
    public virtual IdentityError InvalidToken()
    {
        return new IdentityError
        {
            Code = nameof(InvalidToken),
            Description = Resources.InvalidToken
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a recovery code was not redeemed.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a recovery code was not redeemed.</returns>
    public virtual IdentityError RecoveryCodeRedemptionFailed()
    {
        return new IdentityError
        {
            Code = nameof(RecoveryCodeRedemptionFailed),
            Description = Resources.RecoveryCodeRedemptionFailed
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating an external login is already associated with an account.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating an external login is already associated with an account.</returns>
    public virtual IdentityError LoginAlreadyAssociated()
    {
        return new IdentityError
        {
            Code = nameof(LoginAlreadyAssociated),
            Description = Resources.LoginAlreadyAssociated
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified user <paramref name="userName"/> is invalid.
    /// </summary>
    /// <param name="userName">The user name that is invalid.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specified user <paramref name="userName"/> is invalid.</returns>
    public virtual IdentityError InvalidUserName(string? userName)
    {
        return new IdentityError
        {
            Code = nameof(InvalidUserName),
            Description = Resources.FormatInvalidUserName(userName)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified <paramref name="email"/> is invalid.
    /// </summary>
    /// <param name="email">The email that is invalid.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specified <paramref name="email"/> is invalid.</returns>
    public virtual IdentityError InvalidEmail(string? email)
    {
        return new IdentityError
        {
            Code = nameof(InvalidEmail),
            Description = Resources.FormatInvalidEmail(email)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified <paramref name="userName"/> already exists.
    /// </summary>
    /// <param name="userName">The user name that already exists.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specified <paramref name="userName"/> already exists.</returns>
    public virtual IdentityError DuplicateUserName(string userName)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateUserName),
            Description = Resources.FormatDuplicateUserName(userName)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified <paramref name="email"/> is already associated with an account.
    /// </summary>
    /// <param name="email">The email that is already associated with an account.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specified <paramref name="email"/> is already associated with an account.</returns>
    public virtual IdentityError DuplicateEmail(string email)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateEmail),
            Description = Resources.FormatDuplicateEmail(email)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified <paramref name="role"/> name is invalid.
    /// </summary>
    /// <param name="role">The invalid role.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specific role <paramref name="role"/> name is invalid.</returns>
    public virtual IdentityError InvalidRoleName(string? role)
    {
        return new IdentityError
        {
            Code = nameof(InvalidRoleName),
            Description = Resources.FormatInvalidRoleName(role)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating the specified <paramref name="role"/> name already exists.
    /// </summary>
    /// <param name="role">The duplicate role.</param>
    /// <returns>An <see cref="IdentityError"/> indicating the specific role <paramref name="role"/> name already exists.</returns>
    public virtual IdentityError DuplicateRoleName(string role)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateRoleName),
            Description = Resources.FormatDuplicateRoleName(role)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a user already has a password.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a user already has a password.</returns>
    public virtual IdentityError UserAlreadyHasPassword()
    {
        return new IdentityError
        {
            Code = nameof(UserAlreadyHasPassword),
            Description = Resources.UserAlreadyHasPassword
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating user lockout is not enabled.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating user lockout is not enabled.</returns>
    public virtual IdentityError UserLockoutNotEnabled()
    {
        return new IdentityError
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = Resources.UserLockoutNotEnabled
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a user is already in the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The duplicate role.</param>
    /// <returns>An <see cref="IdentityError"/> indicating a user is already in the specified <paramref name="role"/>.</returns>
    public virtual IdentityError UserAlreadyInRole(string role)
    {
        return new IdentityError
        {
            Code = nameof(UserAlreadyInRole),
            Description = Resources.FormatUserAlreadyInRole(role)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a user is not in the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The duplicate role.</param>
    /// <returns>An <see cref="IdentityError"/> indicating a user is not in the specified <paramref name="role"/>.</returns>
    public virtual IdentityError UserNotInRole(string role)
    {
        return new IdentityError
        {
            Code = nameof(UserNotInRole),
            Description = Resources.FormatUserNotInRole(role)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password of the specified <paramref name="length"/> does not meet the minimum length requirements.
    /// </summary>
    /// <param name="length">The length that is not long enough.</param>
    /// <returns>An <see cref="IdentityError"/> indicating a password of the specified <paramref name="length"/> does not meet the minimum length requirements.</returns>
    public virtual IdentityError PasswordTooShort(int length)
    {
        return new IdentityError
        {
            Code = nameof(PasswordTooShort),
            Description = Resources.FormatPasswordTooShort(length)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password does not meet the minimum number <paramref name="uniqueChars"/> of unique chars.
    /// </summary>
    /// <param name="uniqueChars">The number of different chars that must be used.</param>
    /// <returns>An <see cref="IdentityError"/> indicating a password does not meet the minimum number <paramref name="uniqueChars"/> of unique chars.</returns>
    public virtual IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = Resources.FormatPasswordRequiresUniqueChars(uniqueChars)
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password entered does not contain a non-alphanumeric character, which is required by the password policy.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a password entered does not contain a non-alphanumeric character.</returns>
    public virtual IdentityError PasswordRequiresNonAlphanumeric()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = Resources.PasswordRequiresNonAlphanumeric
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password entered does not contain a numeric character, which is required by the password policy.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a password entered does not contain a numeric character.</returns>
    public virtual IdentityError PasswordRequiresDigit()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresDigit),
            Description = Resources.PasswordRequiresDigit
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password entered does not contain a lower case letter, which is required by the password policy.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a password entered does not contain a lower case letter.</returns>
    public virtual IdentityError PasswordRequiresLower()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresLower),
            Description = Resources.PasswordRequiresLower
        };
    }

    /// <summary>
    /// Returns an <see cref="IdentityError"/> indicating a password entered does not contain an upper case letter, which is required by the password policy.
    /// </summary>
    /// <returns>An <see cref="IdentityError"/> indicating a password entered does not contain an upper case letter.</returns>
    public virtual IdentityError PasswordRequiresUpper()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUpper),
            Description = Resources.PasswordRequiresUpper
        };
    }
}
