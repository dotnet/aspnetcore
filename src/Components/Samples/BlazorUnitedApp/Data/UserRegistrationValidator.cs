// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorUnitedApp.Services;
using FluentValidation;

namespace BlazorUnitedApp.Data;

public class UserRegistrationValidator : AbstractValidator<UserRegistration>
{
    public UserRegistrationValidator(UserAvailabilityService availability)
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(20).WithMessage("Username must be 20 characters or fewer.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, '_' and '-'.")
            .MustAsync(availability.IsUsernameAvailableAsync).WithMessage("This username is already taken.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MustAsync(availability.IsEmailDomainReachableAsync).WithMessage("Email domain is not allowed.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm the password.")
            .Equal(x => x.Password).WithMessage("The passwords do not match.");

        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Street is required.")
            .MaximumLength(100).WithMessage("Street must be 100 characters or fewer.");

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(60).WithMessage("City must be 60 characters or fewer.");
    }
}
