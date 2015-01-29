// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    public class IdentityErrorDescriber
    {
        public static IdentityErrorDescriber Default = new IdentityErrorDescriber();

        public virtual IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description = Resources.DefaultError
            };
        }
        public virtual IdentityError ConcurrencyFailure()
        {
            return new IdentityError
            {
                Code = nameof(ConcurrencyFailure),
                Description = Resources.ConcurrencyFailure
            };
        }

        public virtual IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = Resources.PasswordMismatch
            };
        }

        public virtual IdentityError InvalidToken()
        {
            return new IdentityError
            {
                Code = nameof(InvalidToken),
                Description = Resources.InvalidToken
            };
        }

        public virtual IdentityError LoginAlreadyAssociated()
        {
            return new IdentityError
            {
                Code = nameof(LoginAlreadyAssociated),
                Description = Resources.LoginAlreadyAssociated
            };
        }

        public virtual IdentityError InvalidUserName(string name)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = Resources.FormatInvalidUserName(name)
            };
        }

        public virtual IdentityError InvalidEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = Resources.FormatInvalidEmail(email)
            };
        }

        public virtual IdentityError DuplicateUserName(string name)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = Resources.FormatDuplicateUserName(name)
            };
        }

        public virtual IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = Resources.FormatDuplicateEmail(email)
            };
        }

        public virtual IdentityError InvalidRoleName(string name)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                Description = Resources.FormatInvalidRoleName(name)
            };
        }

        public virtual IdentityError DuplicateRoleName(string name)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                Description = Resources.FormatDuplicateRoleName(name)
            };
        }

        public virtual IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyHasPassword),
                Description = Resources.UserAlreadyHasPassword
            };
        }

        public virtual IdentityError UserLockoutNotEnabled()
        {
            return new IdentityError
            {
                Code = nameof(UserLockoutNotEnabled),
                Description = Resources.UserLockoutNotEnabled
            };
        }

        public virtual IdentityError UserAlreadyInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyInRole),
                Description = Resources.FormatUserAlreadyInRole(role)
            };
        }

        public virtual IdentityError UserNotInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserNotInRole),
                Description = Resources.FormatUserNotInRole(role)
            };
        }

        public virtual IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = Resources.FormatPasswordTooShort(length)
            };
        }

        public virtual IdentityError PasswordRequiresNonLetterAndDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonLetterAndDigit),
                Description = Resources.PasswordRequiresNonLetterAndDigit
            };
        }

        public virtual IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = Resources.PasswordRequiresDigit
            };
        }

        public virtual IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = Resources.PasswordRequiresLower
            };
        }

        public virtual IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = Resources.PasswordRequiresUpper
            };
        }
    }
}