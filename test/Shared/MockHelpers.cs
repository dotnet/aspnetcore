using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Moq;

namespace Microsoft.AspNet.Identity.Test
{
    public static class MockHelpers
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var options = new OptionsAccessor<IdentityOptions>(null);
            return new Mock<UserManager<TUser>>(new ServiceCollection().BuildServiceProvider(), store.Object, options);
        }

        public static UserManager<TUser> TestUserManager<TUser>() where TUser : class
        {
            return TestUserManager(new Mock<IUserStore<TUser>>().Object);
        }

        public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store) where TUser : class
        {
            var options = new OptionsAccessor<IdentityOptions>(null);
            var validator = new Mock<UserValidator<TUser>>();
            var userManager = new UserManager<TUser>(new ServiceCollection().BuildServiceProvider(), store, options);
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>(), CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;
            return userManager;
        }
    }
}