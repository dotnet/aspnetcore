// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization.Test;

public class DefaultAuthorizationServiceTests
{
    private IAuthorizationService BuildAuthorizationService(Action<IServiceCollection> setupServices = null)
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddLogging();
        services.AddOptions();
        setupServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    [Fact]
    public async Task AuthorizeCombineThrowsOnUnknownPolicy()
    {
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => AuthorizationPolicy.CombineAsync(provider, new AuthorizeAttribute[] {
                new AuthorizeAttribute { Policy = "Wut" }
            }));
    }

    [Fact]
    public async Task Authorize_ShouldAllowIfClaimIsPresent()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim("Permission", "CanViewPage") }));

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldAllowIfClaimIsPresentWithSpecifiedAuthType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy =>
            {
                policy.AddAuthenticationSchemes("Basic");
                policy.RequireClaim("Permission", "CanViewPage");
            });
        });
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim("Permission", "CanViewPage") }, "Basic"));

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldAllowIfClaimIsAmongValues()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage", "CanViewAnything")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Permission", "CanViewPage"),
                        new Claim("Permission", "CanViewAnything")
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldInvokeAllHandlersByDefault()
    {
        // Arrange
        var handler1 = new FailHandler();
        var handler2 = new FailHandler();
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(handler1);
            services.AddSingleton<IAuthorizationHandler>(handler2);
            services.AddAuthorizationBuilder().AddPolicy("Custom", policy => policy.Requirements.Add(new CustomRequirement()));
        });

        // Act
        var allowed = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), "Custom");

        // Assert
        Assert.False(allowed.Succeeded);
        Assert.True(allowed.Failure.FailCalled);
        Assert.True(handler1.Invoked);
        Assert.True(handler2.Invoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authorize_ShouldInvokeAllHandlersDependingOnSetting(bool invokeAllHandlers)
    {
        // Arrange
        var handler1 = new FailHandler();
        var handler2 = new FailHandler();
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(handler1);
            services.AddSingleton<IAuthorizationHandler>(handler2);
            services.AddAuthorizationBuilder()
            .AddPolicy("Custom", policy => policy.Requirements.Add(new CustomRequirement()))
            .SetInvokeHandlersAfterFailure(invokeAllHandlers);
        });

        // Act
        var allowed = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), "Custom");

        // Assert
        Assert.False(allowed.Succeeded);
        Assert.True(handler1.Invoked);
        Assert.Equal(invokeAllHandlers, handler2.Invoked);
    }

    private class FailHandler : IAuthorizationHandler
    {
        public bool Invoked { get; set; }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            Invoked = true;
            context.Fail();
            return Task.FromResult(0);
        }
    }

    private class ReasonableFailHandler : IAuthorizationHandler
    {
        private string _reason;

        public ReasonableFailHandler(string reason) => _reason = reason;

        public bool Invoked { get; set; }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            Invoked = true;
            context.Fail(new AuthorizationFailureReason(this, _reason));
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task CanFailWithReasons()
    {
        var handler1 = new ReasonableFailHandler("1");
        var handler2 = new FailHandler();
        var handler3 = new ReasonableFailHandler("3");
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(handler1);
            services.AddSingleton<IAuthorizationHandler>(handler2);
            services.AddSingleton<IAuthorizationHandler>(handler3);
            services.AddAuthorizationBuilder().AddPolicy("Custom", policy => policy.Requirements.Add(new CustomRequirement()));
        });

        // Act
        var allowed = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), "Custom");

        // Assert
        Assert.False(allowed.Succeeded);
        Assert.NotNull(allowed.Failure);
        Assert.Equal(2, allowed.Failure.FailureReasons.Count());
        var first = allowed.Failure.FailureReasons.First();
        Assert.Equal("1", first.Message);
        Assert.Equal(handler1, first.Handler);
        var second = allowed.Failure.FailureReasons.Last();
        Assert.Equal("3", second.Message);
        Assert.Equal(handler3, second.Handler);
    }

    [Fact]
    public async Task Authorize_ShouldFailWhenAllRequirementsNotHandled()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage", "CanViewAnything")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("SomethingElse", "CanViewPage"),
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
        Assert.IsType<ClaimsAuthorizationRequirement>(allowed.Failure.FailedRequirements.First());
    }

    [Fact]
    public async Task Authorize_ShouldNotAllowIfClaimTypeIsNotPresent()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage", "CanViewAnything")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("SomethingElse", "CanViewPage"),
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldNotAllowIfClaimValueIsNotPresent()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Permission", "CanViewComment"),
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldNotAllowIfNoClaims()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[0],
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldNotAllowIfUserIsNull()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));

        // Act
        var allowed = await authorizationService.AuthorizeAsync(null, null, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldNotAllowIfNotCorrectAuthType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ShouldAllowWithNoAuthType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Permission", "CanViewPage"),
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_ThrowsWithUnknownPolicy()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(new ClaimsPrincipal(), "whatever", "BogusPolicy"));
        Assert.Equal("No policy found: BogusPolicy.", exception.Message);
    }

    [Fact]
    public async Task Authorize_CustomRolePolicy()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireRole("Administrator")
            .RequireClaim(ClaimTypes.Role, "User");
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim(ClaimTypes.Role, "Administrator")
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, policy.Build());

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_HasAnyClaimOfTypePolicy()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireClaim(ClaimTypes.Role);
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim(ClaimTypes.Role, "none"),
                },
                "Basic")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, policy.Build());

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task Authorize_PolicyCanAuthenticationSchemeWithNameClaim()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder("AuthType").RequireClaim(ClaimTypes.Name);
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "Name") }, "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, policy.Build());

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task RolePolicyCanRequireSingleRole()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder("AuthType").RequireRole("Admin");
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "Admin") }, "AuthType")
        );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, policy.Build());

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task RolePolicyCanRequireOneOfManyRoles()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder("AuthType").RequireRole("Admin", "Users");
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "Users") }, "AuthType"));

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, policy.Build());

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task RolePolicyCanBlockWrongRole()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireClaim("Permission", "CanViewPage");
        var authorizationService = BuildAuthorizationService();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim(ClaimTypes.Role, "Nope"),
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, policy.Build());

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task RolePolicyCanBlockNoRole()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => policy.RequireRole("Admin", "Users")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public void PolicyThrowsWithNoRequirements()
    {
        Assert.Throws<InvalidOperationException>(() => BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Basic", policy => { })));
    }

    [Fact]
    public async Task RequireUserNameFailsForWrongUserName()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
            services.AddAuthorizationBuilder().AddPolicy("Hao", policy => policy.RequireUserName("Hao")));
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim(ClaimTypes.Name, "Tek"),
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Hao");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task CanRequireUserName()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Hao", policy => policy.RequireUserName("Hao"));
            });
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim(ClaimTypes.Name, "Hao"),
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Hao");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CanRequireUserNameWithDiffClaimType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Hao", policy => policy.RequireUserName("Hao"));
            });
        });
        var identity = new ClaimsIdentity("AuthType", "Name", "Role");
        identity.AddClaim(new Claim("Name", "Hao"));
        var user = new ClaimsPrincipal(identity);

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Hao");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CanRequireRoleWithDiffClaimType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Hao", policy => policy.RequireRole("Hao"));
            });
        });
        var identity = new ClaimsIdentity("AuthType", "Name", "Role");
        identity.AddClaim(new Claim("Role", "Hao"));
        var user = new ClaimsPrincipal(identity);

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Hao");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CanApproveAnyAuthenticatedUser()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Any", policy => policy.RequireAuthenticatedUser());
            });
        });
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        user.AddIdentity(new ClaimsIdentity(
            new Claim[] {
                    new Claim(ClaimTypes.Name, "Name"),
            },
            "AuthType"));

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Any");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CanBlockNonAuthenticatedUser()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Any", policy => policy.RequireAuthenticatedUser());
            });
        });
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Any");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    public class CustomRequirement : IAuthorizationRequirement { }
    public class CustomHandler : AuthorizationHandler<CustomRequirement>
    {
        public bool Invoked { get; set; }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomRequirement requirement)
        {
            Invoked = true;
            context.Succeed(requirement);
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task CustomReqWithNoHandlerFails()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Custom", policy => policy.Requirements.Add(new CustomRequirement()));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Custom");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task CustomReqWithHandlerSucceeds()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddTransient<IAuthorizationHandler, CustomHandler>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Custom", policy => policy.Requirements.Add(new CustomRequirement()));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Custom");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    public class PassThroughRequirement : AuthorizationHandler<PassThroughRequirement>, IAuthorizationRequirement
    {
        public PassThroughRequirement(bool succeed)
        {
            Succeed = succeed;
        }

        public bool Succeed { get; set; }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PassThroughRequirement requirement)
        {
            if (Succeed)
            {
                context.Succeed(requirement);
            }
            return Task.FromResult(0);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PassThroughRequirementWillSucceedWithoutCustomHandler(bool shouldSucceed)
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Passthrough", policy => policy.Requirements.Add(new PassThroughRequirement(shouldSucceed)));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Passthrough");

        // Assert
        Assert.Equal(shouldSucceed, allowed.Succeeded);
    }

    [Fact]
    public async Task CanCombinePolicies()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                var basePolicy = new AuthorizationPolicyBuilder().RequireClaim("Base", "Value").Build();
                options.AddPolicy("Combined", policy => policy.Combine(basePolicy).RequireClaim("Claim", "Exists"));
            });
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Base", "Value"),
                        new Claim("Claim", "Exists")
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Combined");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CombinePoliciesWillFailIfBasePolicyFails()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                var basePolicy = new AuthorizationPolicyBuilder().RequireClaim("Base", "Value").Build();
                options.AddPolicy("Combined", policy => policy.Combine(basePolicy).RequireClaim("Claim", "Exists"));
            });
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Claim", "Exists")
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Combined");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    [Fact]
    public async Task CombinedPoliciesWillFailIfExtraRequirementFails()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                var basePolicy = new AuthorizationPolicyBuilder().RequireClaim("Base", "Value").Build();
                options.AddPolicy("Combined", policy => policy.Combine(basePolicy).RequireClaim("Claim", "Exists"));
            });
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("Base", "Value"),
                },
                "AuthType")
            );

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, null, "Combined");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    public class ExpenseReport { }

    public static class Operations
    {
        public static OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement { Name = "Edit" };
        public static OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = "Create" };
        public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = "Delete" };
    }

    public class ExpenseReportAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, ExpenseReport>
    {
        public ExpenseReportAuthorizationHandler(IEnumerable<OperationAuthorizationRequirement> authorized)
        {
            _allowed = authorized;
        }

        private readonly IEnumerable<OperationAuthorizationRequirement> _allowed;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, ExpenseReport resource)
        {
            if (_allowed.Contains(requirement))
            {
                context.Succeed(requirement);
            }
            return Task.FromResult(0);
        }
    }

    public class SuperUserHandler : AuthorizationHandler<OperationAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement)
        {
            if (context.User.HasClaim("SuperUser", "yes"))
            {
                context.Succeed(requirement);
            }
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task CanAuthorizeAllSuperuserOperations()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(new ExpenseReportAuthorizationHandler(new OperationAuthorizationRequirement[] { Operations.Edit }));
            services.AddTransient<IAuthorizationHandler, SuperUserHandler>();
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("SuperUser", "yes"),
                },
                "AuthType")
            );

        // Act
        // Assert
        Assert.True((await authorizationService.AuthorizeAsync(user, null, Operations.Edit)).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(user, null, Operations.Delete)).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(user, null, Operations.Create)).Succeeded);
    }

    public class NotCalledHandler : AuthorizationHandler<OperationAuthorizationRequirement, string>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, string resource)
        {
            throw new NotImplementedException();
        }
    }

    public class EvenHandler : AuthorizationHandler<OperationAuthorizationRequirement, int>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, int id)
        {
            if (id % 2 == 0)
            {
                context.Succeed(requirement);
            }
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task CanUseValueTypeResource()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddTransient<IAuthorizationHandler, EvenHandler>();
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                },
                "AuthType")
            );

        // Act
        // Assert
        Assert.False((await authorizationService.AuthorizeAsync(user, 1, Operations.Edit)).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(user, 2, Operations.Edit)).Succeeded);
    }

    [Fact]
    public async Task DoesNotCallHandlerWithWrongResourceType()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddTransient<IAuthorizationHandler, NotCalledHandler>();
        });
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[] {
                        new Claim("SuperUser", "yes")
                },
                "AuthType")
            );

        // Act
        // Assert
        Assert.False((await authorizationService.AuthorizeAsync(user, 1, Operations.Edit)).Succeeded);
    }

    [Fact]
    public async Task CanAuthorizeOnlyAllowedOperations()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(new ExpenseReportAuthorizationHandler(new OperationAuthorizationRequirement[] { Operations.Edit }));
        });
        var user = new ClaimsPrincipal();

        // Act
        // Assert
        Assert.True((await authorizationService.AuthorizeAsync(user, new ExpenseReport(), Operations.Edit)).Succeeded);
        Assert.False((await authorizationService.AuthorizeAsync(user, new ExpenseReport(), Operations.Delete)).Succeeded);
        Assert.False((await authorizationService.AuthorizeAsync(user, new ExpenseReport(), Operations.Create)).Succeeded);
    }

    [Fact]
    public async Task AuthorizeHandlerNotCalledWithNullResource()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler>(new ExpenseReportAuthorizationHandler(new OperationAuthorizationRequirement[] { Operations.Edit }));
        });
        var user = new ClaimsPrincipal();

        // Act
        // Assert
        Assert.False((await authorizationService.AuthorizeAsync(user, null, Operations.Edit)).Succeeded);
    }

    [Fact]
    public async Task CanAuthorizeWithAssertionRequirement()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Basic", policy => policy.RequireAssertion(context => true));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task CanAuthorizeWithAsyncAssertionRequirement()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Basic", policy => policy.RequireAssertion(context => Task.FromResult(true)));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.True(allowed.Succeeded);
    }

    public class StaticPolicyProvider : IAuthorizationPolicyProvider
    {
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy>(null);
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            return Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }
    }

    [Fact]
    public async Task CanReplaceDefaultPolicyProvider()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            // This will ignore the policy options
            services.AddSingleton<IAuthorizationPolicyProvider, StaticPolicyProvider>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Basic", policy => policy.RequireAssertion(context => true));
            });
        });
        var user = new ClaimsPrincipal();

        // Act
        var allowed = await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.False(allowed.Succeeded);
    }

    public class DynamicPolicyProvider : IAuthorizationPolicyProvider
    {
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy>(null);
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            return Task.FromResult(new AuthorizationPolicyBuilder().RequireClaim(policyName).Build());
        }
    }

    [Fact]
    public async Task CanUseDynamicPolicyProvider()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            // This will ignore the policy options
            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
            services.AddAuthorization(options => { });
        });
        var id = new ClaimsIdentity();
        id.AddClaim(new Claim("1", "1"));
        id.AddClaim(new Claim("2", "2"));
        var user = new ClaimsPrincipal(id);

        // Act
        // Assert
        Assert.False((await authorizationService.AuthorizeAsync(user, "0")).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(user, "1")).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(user, "2")).Succeeded);
        Assert.False((await authorizationService.AuthorizeAsync(user, "3")).Succeeded);
    }

    public class SuccessEvaluator : IAuthorizationEvaluator
    {
        public AuthorizationResult Evaluate(AuthorizationHandlerContext context) => AuthorizationResult.Success();
    }

    [Fact]
    public async Task CanUseCustomEvaluatorThatOverridesRequirement()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationEvaluator, SuccessEvaluator>();
            services.AddAuthorization(options => options.AddPolicy("Fail", p => p.RequireAssertion(c => false)));
        });
        var result = await authorizationService.AuthorizeAsync(null, "Fail");
        Assert.True(result.Succeeded);
    }

    public class BadContextMaker : IAuthorizationHandlerContextFactory
    {
        public AuthorizationHandlerContext CreateContext(IEnumerable<IAuthorizationRequirement> requirements, ClaimsPrincipal user, object resource)
        {
            return new BadContext();
        }
    }

    public class BadContext : AuthorizationHandlerContext
    {
        public BadContext() : base(new List<IAuthorizationRequirement>(), null, null) { }

        public override bool HasFailed
        {
            get
            {
                return true;
            }
        }

        public override bool HasSucceeded
        {
            get
            {
                return false;
            }
        }
    }

    [Fact]
    public async Task CanUseCustomContextThatAlwaysFails()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandlerContextFactory, BadContextMaker>();
            services.AddAuthorization(options => options.AddPolicy("Success", p => p.RequireAssertion(c => true)));
        });
        Assert.False((await authorizationService.AuthorizeAsync(null, "Success")).Succeeded);
    }

    public class SadHandlerProvider : IAuthorizationHandlerProvider
    {
        public Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context)
        {
            return Task.FromResult<IEnumerable<IAuthorizationHandler>>(new IAuthorizationHandler[1] { new FailHandler() });
        }
    }

    [Fact]
    public async Task CanUseCustomHandlerProvider()
    {
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandlerProvider, SadHandlerProvider>();
            services.AddAuthorization(options => options.AddPolicy("Success", p => p.RequireAssertion(c => true)));
        });
        Assert.False((await authorizationService.AuthorizeAsync(null, "Success")).Succeeded);
    }

    public class LogRequirement : IAuthorizationRequirement
    {
        public override string ToString()
        {
            return "LogRequirement";
        }
    }

    public class DefaultAuthorizationServiceTestLogger : ILogger<DefaultAuthorizationService>
    {
        private readonly Action<LogLevel, EventId, object, Exception, Func<object, Exception, string>> _assertion;

        public DefaultAuthorizationServiceTestLogger(Action<LogLevel, EventId, object, Exception, Func<object, Exception, string>> assertion)
        {
            _assertion = assertion;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _assertion(logLevel, eventId, state, exception, (s, e) => formatter?.Invoke((TState)s, e));
        }
    }

    [Fact]
    public async Task Authorize_ShouldLogRequirementDetailWhenUnHandled()
    {
        // Arrange

        static void Assertion(LogLevel level, EventId eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            Assert.Equal(LogLevel.Information, level);
            Assert.Equal(2, eventId.Id);
            Assert.Equal("UserAuthorizationFailed", eventId.Name);
            var message = formatter(state, exception);

            Assert.Equal("Authorization failed. These requirements were not met:" + Environment.NewLine + "LogRequirement" + Environment.NewLine + "LogRequirement", message);
        }

        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<ILogger<DefaultAuthorizationService>>(new DefaultAuthorizationServiceTestLogger(Assertion));
            services.AddAuthorization(options => options.AddPolicy("Log", p =>
            {
                p.Requirements.Add(new LogRequirement());
                p.Requirements.Add(new LogRequirement());
            }));
        });

        var user = new ClaimsPrincipal();

        // Act
        var result = await authorizationService.AuthorizeAsync(user, "Log");

        // Assert
    }

    [Fact]
    public async Task Authorize_ShouldLogExplicitFailedWhenFailedCall()
    {
        // Arrange

        static void Assertion(LogLevel level, EventId eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            Assert.Equal(LogLevel.Information, level);
            Assert.Equal(2, eventId.Id);
            Assert.Equal("UserAuthorizationFailed", eventId.Name);
            var message = formatter(state, exception);

            Assert.Equal("Authorization failed. Fail() was explicitly called.", message);
        }

        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddSingleton<IAuthorizationHandler, FailHandler>();
            services.AddSingleton<ILogger<DefaultAuthorizationService>>(new DefaultAuthorizationServiceTestLogger(Assertion));
            services.AddAuthorization(options => options.AddPolicy("Log", p =>
            {
                p.Requirements.Add(new LogRequirement());
                p.Requirements.Add(new LogRequirement());
            }));
        });

        var user = new ClaimsPrincipal();

        // Act
        var result = await authorizationService.AuthorizeAsync(user, "Log");

        // Assert
    }
}
