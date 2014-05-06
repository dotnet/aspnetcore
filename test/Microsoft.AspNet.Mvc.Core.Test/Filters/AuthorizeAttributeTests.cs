// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Security;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AuthorizeAttributeTests : AuthorizeAttributeTestsBase
    {
        [Fact]
        public async Task Invoke_ValidClaimShouldNotFail()
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
        public async Task Invoke_EmptyClaimsShouldRejectAnonymousUser()
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
        public async Task Invoke_EmptyClaimsWithAllowAnonymousAttributeShouldNotRejectAnonymousUser()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var authorizeAttribute = new AuthorizeAttribute();
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService),
                anonymous: true
                );

            authorizationContext.Filters.Add(new AllowAnonymousAttribute());

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_EmptyClaimsShouldAuthorizeAuthenticatedUser()
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
        public async Task Invoke_SingleValidClaimShouldSucceed()
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
        public async Task Invoke_InvalidClaimShouldFail()
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
        public async Task Invoke_FailedContextShouldNotCheckPermission()
        {
            // Arrange
            bool authorizationServiceIsCalled = false;
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(Enumerable.Empty<Claim>(), null, null))
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

        [Fact]
        public async Task Invoke_NullPoliciesShouldNotFail()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(policies: null);
            var authorizeAttribute = new AuthorizeAttribute("Permission", "CanViewPage");
            var authorizationContext = GetAuthorizationContext(services => 
                services.AddInstance<IAuthorizationService>(authorizationService)
                );

            // Act
            await authorizeAttribute.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }
    }
}
