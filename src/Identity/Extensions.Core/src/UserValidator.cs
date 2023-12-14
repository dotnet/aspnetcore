// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides validation services for user classes.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public class UserValidator<TUser> : IUserValidator<TUser> where TUser : class
{
    /// <summary>
    /// Creates a new instance of <see cref="UserValidator{TUser}"/>.
    /// </summary>
    /// <param name="errors">The <see cref="IdentityErrorDescriber"/> used to provider error messages.</param>
    public UserValidator(IdentityErrorDescriber? errors = null)
    {
        Describer = errors ?? new IdentityErrorDescriber();
    }

    /// <summary>
    /// Gets the <see cref="IdentityErrorDescriber"/> used to provider error messages for the current <see cref="UserValidator{TUser}"/>.
    /// </summary>
    /// <value>The <see cref="IdentityErrorDescriber"/> used to provider error messages for the current <see cref="UserValidator{TUser}"/>.</value>
    public IdentityErrorDescriber Describer { get; private set; }

    /// <summary>
    /// Validates the specified <paramref name="user"/> as an asynchronous operation.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user to validate.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the validation operation.</returns>
    public virtual async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
    {
        ArgumentNullThrowHelper.ThrowIfNull(manager);
        ArgumentNullThrowHelper.ThrowIfNull(user);
        var errors = await ValidateUserName(manager, user).ConfigureAwait(false);
        if (manager.Options.User.RequireUniqueEmail)
        {
            errors = await ValidateEmail(manager, user, errors).ConfigureAwait(false);
        }
        return errors?.Count > 0 ? IdentityResult.Failed(errors) : IdentityResult.Success;
    }

    private async Task<List<IdentityError>?> ValidateUserName(UserManager<TUser> manager, TUser user)
    {
        List<IdentityError>? errors = null;
        var userName = await manager.GetUserNameAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userName))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.InvalidUserName(userName));
        }
        else if (!string.IsNullOrEmpty(manager.Options.User.AllowedUserNameCharacters) &&
            userName.Any(c => !manager.Options.User.AllowedUserNameCharacters.Contains(c)))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.InvalidUserName(userName));
        }
        else
        {
            var owner = await manager.FindByNameAsync(userName).ConfigureAwait(false);
            if (owner != null &&
                !string.Equals(await manager.GetUserIdAsync(owner).ConfigureAwait(false), await manager.GetUserIdAsync(user).ConfigureAwait(false)))
            {
                errors ??= new List<IdentityError>();
                errors.Add(Describer.DuplicateUserName(userName));
            }
        }

        return errors;
    }

    // make sure email is not empty, valid, and unique
    private async Task<List<IdentityError>?> ValidateEmail(UserManager<TUser> manager, TUser user, List<IdentityError>? errors)
    {
        var email = await manager.GetEmailAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(email))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.InvalidEmail(email));
            return errors;
        }
        if (!new EmailAddressAttribute().IsValid(email))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.InvalidEmail(email));
            return errors;
        }
        var owner = await manager.FindByEmailAsync(email).ConfigureAwait(false);
        if (owner != null &&
            !string.Equals(await manager.GetUserIdAsync(owner).ConfigureAwait(false), await manager.GetUserIdAsync(user).ConfigureAwait(false)))
        {
            errors ??= new List<IdentityError>();
            errors.Add(Describer.DuplicateEmail(email));
        }
        return errors;
    }
}
