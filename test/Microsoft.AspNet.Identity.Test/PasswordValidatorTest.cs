using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class PasswordValidatorTest
    {
        [Fact]
        public async Task ValidateThrowsWithNullTest()
        {
            // Setup
            var validator = new PasswordValidator();

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => validator.Validate(null));
        }


        [Theory, 
        InlineData(""), 
        InlineData("abc"), 
        InlineData("abcde")]
        public async Task FailsIfTooShortTests(string input)
        {
            const string error = "Passwords must be at least 6 characters.";
            var valid = new PasswordValidator {RequiredLength = 6};
            IdentityResultAssert.IsFailure(await valid.Validate(input), error);
        }

        [Theory, 
        InlineData("abcdef"), 
        InlineData("aaaaaaaaaaa")]
        public async Task SuccessIfLongEnoughTests(string input) {
            var valid = new PasswordValidator {RequiredLength = 6};
            IdentityResultAssert.IsSuccess(await valid.Validate(input));
        }

        [Theory, 
        InlineData("a"), 
        InlineData("aaaaaaaaaaa")]
        public async Task FailsWithoutRequiredNonAlphanumericTests(string input)
        {
            var valid = new PasswordValidator { RequireNonLetterOrDigit = true };
            IdentityResultAssert.IsFailure(await valid.Validate(input), "Passwords must have at least one non letter or digit character.");
        }

        [Theory, 
        InlineData("@"), 
        InlineData("abcd@e!ld!kajfd"), 
        InlineData("!!!!!!")]
        public async Task SucceedsWithRequiredNonAlphanumericTests(string input)
        {
            var valid = new PasswordValidator { RequireNonLetterOrDigit = true };
            IdentityResultAssert.IsSuccess(await valid.Validate(input));
        }

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

        [Theory,
        InlineData("abcde", Errors.Length | Errors.Alpha | Errors.Upper | Errors.Digit),
        InlineData("a@B@cd", Errors.Digit),
        InlineData("___", Errors.Length | Errors.Digit | Errors.Lower | Errors.Upper),
        InlineData("a_b9de", Errors.Upper),
        InlineData("abcd@e!ld!kaj9Fd", Errors.None),
        InlineData("aB1@df", Errors.None)]
        public async Task UberMixedRequiredTests(string input, Errors errorMask)
        {
            const string alphaError = "Passwords must have at least one non letter or digit character.";
            const string upperError = "Passwords must have at least one uppercase ('A'-'Z').";
            const string lowerError = "Passwords must have at least one lowercase ('a'-'z').";
            const string digitError = "Passwords must have at least one digit ('0'-'9').";
            const string lengthError = "Passwords must be at least 6 characters.";
            var valid = new PasswordValidator
            {
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequiredLength = 6
            };
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
            if (errors.Count == 0)
            {
                IdentityResultAssert.IsSuccess(await valid.Validate(input));
            }
            else
            {
                IdentityResultAssert.IsFailure(await valid.Validate(input), string.Join(" ", errors));
            }
        }
    }
}