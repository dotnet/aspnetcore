// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Security.Test
{
    public class DefaultAuthorizationServiceTests
    {
        private IAuthorizationService BuildAuthorizationService(Action<IServiceCollection> setupServices = null)
        {
            var services = new ServiceCollection();
            services.AddAuthorization();
            if (setupServices != null)
            {
                setupServices(services);
            }
            return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        private Mock<HttpContext> SetupContext(params ClaimsIdentity[] ids)
        {
            var context = new Mock<HttpContext>();
            context.SetupProperty(c => c.User);
            var user = new ClaimsPrincipal();
            user.AddIdentities(ids);
            context.Object.User = user;
            if (ids != null)
            {
                var results = new List<AuthenticationResult>();
                foreach (var id in ids)
                {
                    results.Add(new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription()));
                }
                context.Setup(c => c.AuthenticateAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(results).Verifiable();
            }
            return context;
        }

        [Fact]
        public async Task Authorize_ShouldAllowIfClaimIsPresent()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.AddPolicy("Basic", new AuthorizationPolicyBuilder()
                        .RequiresClaim("Permission", "CanViewPage")
                        .Build());
                });
            });
            var context = SetupContext(new ClaimsIdentity(new Claim[] { new Claim("Permission", "CanViewPage") }, "Basic"));

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldAllowIfClaimIsPresentWithSpecifiedAuthType()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder("Basic").RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(new ClaimsIdentity(new Claim[] { new Claim("Permission", "CanViewPage") }, "Basic"));

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldAllowIfClaimIsAmongValues()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage", "CanViewAnything");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CanViewPage"),
                        new Claim("Permission", "CanViewAnything")
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldFailWhenAllRequirementsNotHandled()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage", "CanViewAnything");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("SomethingElse", "CanViewPage"),
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfClaimTypeIsNotPresent()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage", "CanViewAnything");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("SomethingElse", "CanViewPage"),
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfClaimValueIsNotPresent()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CanViewComment"),
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfNoClaims()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[0],
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfUserIsNull()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext();
            context.Object.User = null;

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfNotCorrectAuthType()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder("Basic").RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(new ClaimsIdentity());

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldAllowWithNoAuthType()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CanViewPage"),
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_ShouldNotAllowIfUnknownPolicy()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CanViewComment"),
                    },
                    null)
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task Authorize_CustomRolePolicy()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequiresRole("Administrator")
                .RequiresClaim(ClaimTypes.Role, "User");
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim(ClaimTypes.Role, "Administrator")
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_HasAnyClaimOfTypePolicy()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequiresClaim(ClaimTypes.Role);
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim(ClaimTypes.Role, ""),
                    },
                    "Basic")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task Authorize_PolicyCanAuthenticationTypeWithNameClaim()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder("AuthType").RequiresClaim(ClaimTypes.Name);
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "Name") }, "AuthType")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task RolePolicyCanRequireSingleRole()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder("AuthType").RequiresRole("Admin");
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "Admin") }, "AuthType")
            );

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task RolePolicyCanRequireOneOfManyRoles()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder("AuthType").RequiresRole("Admin", "Users");
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "Users") }, "AuthType"));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task RolePolicyCanBlockWrongRole()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequiresClaim("Permission", "CanViewPage");
            var authorizationService = BuildAuthorizationService();
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim(ClaimTypes.Role, "Nope"),
                    },
                    "AuthType")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync(policy.Build(), context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task RolePolicyCanBlockNoRole()
        {
            // Arrange

            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequiresRole("Admin", "Users");
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                    },
                    "AuthType")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task PolicyFailsWithNoRequirements()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder();
                    options.AddPolicy("Basic", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim(ClaimTypes.Name, "Name"),
                    },
                    "AuthType")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Basic", context.Object);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public async Task CanApproveAnyAuthenticatedUser()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
                    options.AddPolicy("Any", policy.Build());
                });
            });
            var context = SetupContext(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim(ClaimTypes.Name, "Name"),
                    },
                    "AuthType")
                );

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Any", context.Object);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public async Task CanBlockNonAuthenticatedUser()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
                    options.AddPolicy("Any", policy.Build());
                });
            });
            var context = SetupContext(new ClaimsIdentity());

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Any", context.Object);

            // Assert
            Assert.False(allowed);
        }

        public class CustomRequirement : IAuthorizationRequirement { }
        public class CustomHandler : AuthorizationHandler<CustomRequirement>
        {
            public override Task<bool> CheckAsync(AuthorizationContext context, CustomRequirement requirement)
            {
                return Task.FromResult(true);
            }
        }

        [Fact]
        public async Task CustomReqWithNoHandlerFails()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder();
                    policy.Requirements.Add(new CustomRequirement());
                    options.AddPolicy("Custom", policy.Build());
                });
            });
            var context = SetupContext();

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Custom", context.Object);

            // Assert
            Assert.False(allowed);
        }


        [Fact]
        public async Task CustomReqWithHandlerSucceeds()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(services =>
            {
                services.AddTransient<IAuthorizationHandler, CustomHandler>();
                services.Configure<AuthorizationOptions>(options =>
                {
                    var policy = new AuthorizationPolicyBuilder();
                    policy.Requirements.Add(new CustomRequirement());
                    options.AddPolicy("Custom", policy.Build());
                });
            });
            var context = SetupContext();

            // Act
            var allowed = await authorizationService.AuthorizeAsync("Custom", context.Object);

            // Assert
            Assert.True(allowed);
        }

    }
}