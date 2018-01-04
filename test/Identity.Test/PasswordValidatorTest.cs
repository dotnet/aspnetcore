// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class PasswordValidatorTest
    {
        [Flags]
        public enum Errors
        {
            None = 0,
            Length = 2,
            Alpha = 4,
            Upper = 8,
            Lower = 16,
            Digit = 32,
        }

        [Fact]
        public async Task ValidateThrowsWithNullTest()
        {
            // Setup
            var validator = new PasswordValidator<TestUser>();

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>("password", () => validator.ValidateAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("manager", () => validator.ValidateAsync(null, null, "foo"));
        }


        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("abcde")]
        public async Task FailsIfTooShortTests(string input)
        {
            const string error = "Passwords must be at least 6 characters.";
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = false;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            IdentityResultAssert.IsFailure(await valid.ValidateAsync(manager, null, input), error);
        }

        [Theory]
        [InlineData("abcdef")]
        [InlineData("aaaaaaaaaaa")]
        public async Task SuccessIfLongEnoughTests(string input)
        {
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = false;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            IdentityResultAssert.IsSuccess(await valid.ValidateAsync(manager, null, input));
        }

        [Theory]
        [InlineData("a")]
        [InlineData("aaaaaaaaaaa")]
        public async Task FailsWithoutRequiredNonAlphanumericTests(string input)
        {
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = true;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            manager.Options.Password.RequiredLength = 0;
            IdentityResultAssert.IsFailure(await valid.ValidateAsync(manager, null, input),
                "Passwords must have at least one non alphanumeric character.");
        }

        [Theory]
        [InlineData("@")]
        [InlineData("abcd@e!ld!kajfd")]
        [InlineData("!!!!!!")]
        public async Task SucceedsWithRequiredNonAlphanumericTests(string input)
        {
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = true;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            manager.Options.Password.RequiredLength = 0;
            IdentityResultAssert.IsSuccess(await valid.ValidateAsync(manager, null, input));
        }

        [Theory]
        [InlineData("a", 2)]
        [InlineData("aaaaaaaaaaa", 2)]
        [InlineData("abcdabcdabcdabcdabcdabcdabcd", 5)]
        public async Task FailsWithoutRequiredUniqueCharsTests(string input, int uniqueChars)
        {
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = false;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            manager.Options.Password.RequiredLength = 0;
            manager.Options.Password.RequiredUniqueChars = uniqueChars;
            IdentityResultAssert.IsFailure(await valid.ValidateAsync(manager, null, input),
                String.Format("Passwords must use at least {0} different characters.", uniqueChars));
        }

        [Theory]
        [InlineData("12345", 5)]
        [InlineData("aAbBc", 5)]
        [InlineData("aAbBcaAbBcaAbBc", 5)]
        [InlineData("!@#$%", 5)]
        [InlineData("a", 1)]
        [InlineData("this is a long password with many chars", 10)]
        public async Task SucceedsWithRequiredUniqueCharsTests(string input, int uniqueChars)
        {
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            manager.Options.Password.RequireUppercase = false;
            manager.Options.Password.RequireNonAlphanumeric = false;
            manager.Options.Password.RequireLowercase = false;
            manager.Options.Password.RequireDigit = false;
            manager.Options.Password.RequiredLength = 0;
            manager.Options.Password.RequiredUniqueChars = uniqueChars;
            IdentityResultAssert.IsSuccess(await valid.ValidateAsync(manager, null, input));
        }

        [Theory]
        [InlineData("abcde", Errors.Length | Errors.Alpha | Errors.Upper | Errors.Digit)]
        [InlineData("a@B@cd", Errors.Digit)]
        [InlineData("___", Errors.Length | Errors.Digit | Errors.Lower | Errors.Upper)]
        [InlineData("a_b9de", Errors.Upper)]
        [InlineData("abcd@e!ld!kaj9Fd", Errors.None)]
        [InlineData("aB1@df", Errors.None)]
        public async Task UberMixedRequiredTests(string input, Errors errorMask)
        {
            const string alphaError = "Passwords must have at least one non alphanumeric character.";
            const string upperError = "Passwords must have at least one uppercase ('A'-'Z').";
            const string lowerError = "Passwords must have at least one lowercase ('a'-'z').";
            const string digitError = "Passwords must have at least one digit ('0'-'9').";
            const string lengthError = "Passwords must be at least 6 characters.";
            var manager = MockHelpers.TestUserManager<TestUser>();
            var valid = new PasswordValidator<TestUser>();
            var errors = new List<string>();
            if ((errorMask & Errors.Length) != Errors.None)
            {
                errors.Add(lengthError);
            }
            if ((errorMask & Errors.Alpha) != Errors.None)
            {
                errors.Add(alphaError);
            }
            if ((errorMask & Errors.Digit) != Errors.None)
            {
                errors.Add(digitError);
            }
            if ((errorMask & Errors.Lower) != Errors.None)
            {
                errors.Add(lowerError);
            }
            if ((errorMask & Errors.Upper) != Errors.None)
            {
                errors.Add(upperError);
            }
            var result = await valid.ValidateAsync(manager, null, input);
            if (errors.Count == 0)
            {
                IdentityResultAssert.IsSuccess(result);
            }
            else
            {
                IdentityResultAssert.IsFailure(result);
                foreach (var error in errors)
                {
                    Assert.Contains(result.Errors, e => e.Description == error);
                }
            }
        }
    }
}