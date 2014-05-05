using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Security.Authorization;
using Xunit;

namespace Microsoft.AspNet.Security.Test
{
    public class DefaultAuthorizationServiceTests
    {
        [Fact]
        public void Check_ShouldAllowIfClaimIsPresent()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity( new Claim[] { new Claim("Permission", "CanViewPage") }, "Basic")
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public void Check_ShouldAllowIfClaimIsAmongValues()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewPage"), 
                        new Claim("Permission", "CanViewAnything")
                    }, 
                    "Basic")
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public void Check_ShouldNotAllowIfClaimTypeIsNotPresent()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("SomethingElse", "CanViewPage"), 
                    },
                    "Basic")
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Check_ShouldNotAllowIfClaimValueIsNotPresent()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewComment"), 
                    },
                    "Basic")
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Check_ShouldNotAllowIfNoClaims()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[0],
                    "Basic")
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Check_ShouldNotAllowIfUserIsNull()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            ClaimsPrincipal user = null;

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Check_ShouldNotAllowIfUserIsNotAuthenticated()
        {
            // Arrange
            var authorizationService = new DefaultAuthorizationService(Enumerable.Empty<IAuthorizationPolicy>());
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewComment"), 
                    },
                    null)
                );

            // Act
            var allowed = authorizationService.Authorize(new Claim[] { new Claim("Permission", "CanViewPage") }, user);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Check_ShouldApplyPoliciesInOrder()
        {
            // Arrange
            string result = "";
            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    Order = 20,
                    ApplyingAsyncAction = (context) => { result += "20"; }
                },
                new FakePolicy() {
                    Order = -1,
                    ApplyingAsyncAction = (context) => { result += "-1"; }
                },
                new FakePolicy() {
                    Order = 30,
                    ApplyingAsyncAction = (context) => { result += "30"; }
                },
            };

            var authorizationService = new DefaultAuthorizationService(policies);
            
            // Act
            var allowed = authorizationService.Authorize(null, null);

            // Assert
            Assert.Equal("-12030", result);
        }

        [Fact]
        public void Check_ShouldInvokeApplyingApplyAppliedInOrder()
        {
            // Arrange
            string result = "";
            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    Order = 20,
                    ApplyingAsyncAction = (context) => { result += "Applying20"; },
                    ApplyAsyncAction = (context) => { result += "Apply20"; },
                    AppliedAsyncAction = (context) => { result += "Applied20"; }
                },
                new FakePolicy() {
                    Order = -1,
                    ApplyingAsyncAction = (context) => { result += "Applying-1"; },
                    ApplyAsyncAction = (context) => { result += "Apply-1"; },
                    AppliedAsyncAction = (context) => { result += "Applied-1"; }
                },
                new FakePolicy() {
                    Order = 30,
                    ApplyingAsyncAction = (context) => { result += "Applying30"; },
                    ApplyAsyncAction = (context) => { result += "Apply30"; },
                    AppliedAsyncAction = (context) => { result += "Applied30"; }
                },
            };

            var authorizationService = new DefaultAuthorizationService(policies);
            
            // Act
            var allowed = authorizationService.Authorize(null, null);

            // Assert
            Assert.Equal("Applying-1Applying20Applying30Apply-1Apply20Apply30Applied-1Applied20Applied30", result);
        }

        [Fact]
        public void Check_ShouldConvertNullClaimsToEmptyList()
        {
            // Arrange
            IList<Claim> claims = null;
            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    Order = 20,
                    ApplyingAsyncAction = (context) => { claims = context.Claims; }
                }
            };

            var authorizationService = new DefaultAuthorizationService(policies);
            
            // Act
            var allowed = authorizationService.Authorize(null, null);

            // Assert
            Assert.NotNull(claims);
            Assert.Equal(0, claims.Count);
        }

        [Fact]
        public void Check_ShouldThrowWhenPoliciesDontStop()
        {
            // Arrange
            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    ApplyAsyncAction = (context) => { context.Retry = true; }
                }
            };

            var authorizationService = new DefaultAuthorizationService(policies);

            // Act
            // Assert
            Exception ex = Assert.Throws<AggregateException>(() => authorizationService.Authorize(null, null));
        }
 
        [Fact]
        public void Check_ApplyCanMutateCheckedClaims()
        {

            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity( new Claim[] { new Claim("Permission", "CanDeleteComments") }, "Basic")
                );

            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    ApplyAsyncAction = (context) => { 
                        // for instance, if user owns the comment
                        if(!context.Claims.Any(claim => claim.Type == "Permission" && claim.Value == "CanDeleteComments"))
                        {
                            context.Claims.Add(new Claim("Permission", "CanDeleteComments")); 
                            context.Retry = true;
                        }
                    }
                }
            };

            var authorizationService = new DefaultAuthorizationService(policies);
            
            // Act
            var allowed = authorizationService.Authorize(Enumerable.Empty<Claim>(), user);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public void Check_PoliciesCanMutateUsersClaims()
        {

            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[0], "Basic")
                );

            var policies = new IAuthorizationPolicy[] {
                new FakePolicy() {
                    ApplyAsyncAction = (context) => { 
                        if (!context.Authorized) 
                        {
                            context.UserClaims.Add(new Claim("Permission", "CanDeleteComments")); 
                            context.Retry = true;
                        }
                    }
                }
            };

            var authorizationService = new DefaultAuthorizationService(policies);
            
            // Act
            var allowed = authorizationService.Authorize(new Claim("Permission", "CanDeleteComments"), user);

            // Assert
            Assert.True(allowed);
        }
    }
}
