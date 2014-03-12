using System.Collections.Generic;
using System.Security.Claims;
using Moq;
using System;
using System.Linq;
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


        [Theory, InlineData(""), InlineData("abc"), InlineData("abcde")]
        public async Task FailsIfTooShortTests(string input)
        {
            const string error = "Passwords must be at least 6 characters.";
            var valid = new PasswordValidator {RequiredLength = 6};
            UnitTestHelper.IsFailure(await valid.Validate(input), error);
        }

        [Theory, InlineData("abcdef"), InlineData("aaaaaaaaaaa")]
        public async Task SuccessIfLongEnoughTests(string input) {
            var valid = new PasswordValidator {RequiredLength = 6};
            UnitTestHelper.IsSuccess(await valid.Validate("abcdef"));
            UnitTestHelper.IsSuccess(await valid.Validate("abcdeldkajfd"));
        }

        [Theory, InlineData("a"), InlineData("aaaaaaaaaaa")]
        public async Task FailsWithoutRequiredNonAlphanumericTests(string input)
        {
            var valid = new PasswordValidator { RequireNonLetterOrDigit = true };
            UnitTestHelper.IsFailure(await valid.Validate(input), "Passwords must have at least one non letter or digit character.");
        }

        [Theory, InlineData("@"), InlineData("abcd@e!ld!kajfd"), InlineData("!!!!!!")]
        public async Task SucceedsWithRequiredNonAlphanumericTests(string input)
        {
            var valid = new PasswordValidator { RequireNonLetterOrDigit = true };
            UnitTestHelper.IsSuccess(await valid.Validate(input));
        }

        [Fact]
        public async Task UberMixedRequiredTests()
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
            UnitTestHelper.IsFailure(await valid.Validate("abcde"),
                string.Join(" ", lengthError, alphaError, digitError, upperError));
            UnitTestHelper.IsFailure(await valid.Validate("a@B@cd"), digitError);
            UnitTestHelper.IsFailure(await valid.Validate("___"),
                string.Join(" ", lengthError, digitError, lowerError, upperError));
            UnitTestHelper.IsFailure(await valid.Validate("a_b9de"), upperError);
            UnitTestHelper.IsSuccess(await valid.Validate("abcd@e!ld!kaj9Fd"));
        }
    }
}