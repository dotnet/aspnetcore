using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Security.Authorization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AuthorizeAttributeTests : AuthorizeAttributeTestsBase
    {
        [Fact]
        public async void Invoke_ValidClaimShouldNotFail()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute("Permission", "CanViewPage");
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService)
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async void Invoke_EmptyClaimsShouldRejectAnonymousUser()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute();
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService),
                anonymous: true
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async void Invoke_EmptyClaimsShouldAuthorizeAuthenticatedUser()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute();
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService)
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async void Invoke_SingleValidClaimShouldSucceed()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute("Permission", "CanViewComment", "CanViewPage");
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService)
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async void Invoke_InvalidClaimShouldFail()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute("Permission", "CanViewComment");
            var authorizationContext = GetAuthorizationContext(services =>
                services.AddInstance<IAuthorizationService>(authorizationService)
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async void Invoke_FailedContextShouldNotCheckPermission()
        {
            // Arrange
            bool authorizationServiceIsCalled = false;
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(null, null))
                .Returns(() =>
                {
                    authorizationServiceIsCalled = true;
                    return Task.FromResult(true);
                });

            var authorizeAttribute = new AuthorizeAttribute("Permission", "CanViewComment");
            var authorizationContext = GetAuthorizationContext(services =>
                services.AddInstance<IAuthorizationService>(authorizationService.Object)
                );
            
            authorizationContext.Result = new HttpStatusCodeResult(401);

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.False(authorizationServiceIsCalled);
        }
    }
}
