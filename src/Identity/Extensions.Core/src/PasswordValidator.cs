// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides the default password policy for Identity.
/// </summary>
/// <typeparam name="TUser">The type that represents a user.</typeparam>
public class PasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
{
    /// <summary>
    /// Constructions a new instance of <see cref="PasswordValidator{TUser}"/>.
    /// </summary>
    /// <param name="errors">The <see cref="IdentityErrorDescriber"/> to retrieve error text from.</param>
    public PasswordValidator(IdentityErrorDescriber? errors = null)
    {
        Describer = errors ?? new IdentityErrorDescriber();
    }

    /// <summary>
    /// Gets the <see cref="IdentityErrorDescriber"/> used to supply error text.
    /// </summary>
    /// <value>The <see cref="IdentityErrorDescriber"/> used to supply error text.</value>
    public IdentityErrorDescriber Describer { get; private set; }

    /// <summary>
    /// Validates a password as an asynchronous operation.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> properties from.</param>
    /// <param name="user">The user whose password should be validated.</param>
    /// <param name="password">The password supplied for validation</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string? password)
    {
        ArgumentNullThrowHelper.ThrowIfNull(manager);
        List<IdentityError>? errors = null;
        var options = manager.Options.Password;
        password ??= "";
        if (string.IsNullOrWhiteSpace(password) || password.Length < options.RequiredLength)
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordTooShort(options.RequiredLength));
        }
        if (options.RequireNonAlphanumeric && AllLetterOrDigit(password))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordRequiresNonAlphanumeric());
        }
        if (options.RequireDigit && !ContainsDigit(password))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordRequiresDigit());
        }
        if (options.RequireLowercase && !ContainsLower(password))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordRequiresLower());
        }
        if (options.RequireUppercase && !ContainsUpper(password))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordRequiresUpper());
        }
        if (options.RequiredUniqueChars >= 1 && !HasMinimumUniqueChars(password, options.RequiredUniqueChars))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.PasswordRequiresUniqueChars(options.RequiredUniqueChars));
        }
        return
            Task.FromResult(errors?.Count > 0
                ? IdentityResult.Failed(errors)
                : IdentityResult.Success);
    }

    private bool AllLetterOrDigit(string password)
    {
        foreach (var c in password)
        {
            if (!IsLetterOrDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    private bool ContainsDigit(string password)
    {
        foreach (var c in password)
        {
            if (IsDigit(c))
            {
                return true;
            }
        }
        return false;
    }

    private bool ContainsLower(string password)
    {
        foreach (var c in password)
        {
            if (IsLower(c))
            {
                return true;
            }
        }
        return false;
    }

    private bool ContainsUpper(string password)
    {
        foreach (var c in password)
        {
            if (IsUpper(c))
            {
                return true;
            }
        }
        return false;
    }

    // Equivalent to password.Distinct().Count() >= requiredUniqueChars, without allocating for ASCII
    // characters and stopping as soon as enough distinct characters have been seen.
    private static bool HasMinimumUniqueChars(string password, int requiredUniqueChars)
    {
        if (password.Length < requiredUniqueChars)
        {
            return false;
        }

        ulong seenLow = 0; // chars 0-63
        ulong seenHigh = 0; // chars 64-127
        HashSet<char>? seenOther = null;
        var uniqueChars = 0;
        foreach (var c in password)
        {
            bool added;
            if (c < 64)
            {
                var bit = 1UL << c;
                added = (seenLow & bit) == 0;
                seenLow |= bit;
            }
            else if (c < 128)
            {
                var bit = 1UL << (c - 64);
                added = (seenHigh & bit) == 0;
                seenHigh |= bit;
            }
            else
            {
                added = (seenOther ??= new HashSet<char>()).Add(c);
            }

            if (added && ++uniqueChars >= requiredUniqueChars)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns a flag indicating whether the supplied character is a digit.
    /// </summary>
    /// <param name="c">The character to check if it is a digit.</param>
    /// <returns>True if the character is a digit, otherwise false.</returns>
    public virtual bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    /// <summary>
    /// Returns a flag indicating whether the supplied character is a lower case ASCII letter.
    /// </summary>
    /// <param name="c">The character to check if it is a lower case ASCII letter.</param>
    /// <returns>True if the character is a lower case ASCII letter, otherwise false.</returns>
    public virtual bool IsLower(char c)
    {
        return c >= 'a' && c <= 'z';
    }

    /// <summary>
    /// Returns a flag indicating whether the supplied character is an upper case ASCII letter.
    /// </summary>
    /// <param name="c">The character to check if it is an upper case ASCII letter.</param>
    /// <returns>True if the character is an upper case ASCII letter, otherwise false.</returns>
    public virtual bool IsUpper(char c)
    {
        return c >= 'A' && c <= 'Z';
    }

    /// <summary>
    /// Returns a flag indicating whether the supplied character is an ASCII letter or digit.
    /// </summary>
    /// <param name="c">The character to check if it is an ASCII letter or digit.</param>
    /// <returns>True if the character is an ASCII letter or digit, otherwise false.</returns>
    public virtual bool IsLetterOrDigit(char c)
    {
        return IsUpper(c) || IsLower(c) || IsDigit(c);
    }
}
