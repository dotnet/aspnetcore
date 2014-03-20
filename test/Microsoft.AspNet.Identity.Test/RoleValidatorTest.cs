using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class RoleValidatorTest
    {
        [Fact]
        public async Task ValidateThrowsWithNull()
        {
            // Setup
            var manager = new RoleManager<TestRole, string>(new NoopRoleStore());
            var validator = new RoleValidator<TestRole, string>();

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>("manager", async () => await validator.Validate(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await validator.Validate(manager, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValidateFailsWithTooShortRoleName(string input)
        {
            // Setup
            var manager = new RoleManager<TestRole, string>(new NoopRoleStore());
            var validator = new RoleValidator<TestRole, string>();
            var user = new TestRole {Name = input};

            // Act
            var result = await validator.Validate(manager, user);

            // Assert
            IdentityResultAssert.IsFailure(result, "Name cannot be null or empty.");
        }
    }
}