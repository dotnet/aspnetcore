// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class IdentityOptionsTest
{
    [Fact]
    public void VerifyDefaultOptions()
    {
        var options = new IdentityOptions();
        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);

        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.True(options.Password.RequireUppercase);
        Assert.Equal(6, options.Password.RequiredLength);
        Assert.Equal(1, options.Password.RequiredUniqueChars);

        Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+", options.User.AllowedUserNameCharacters);
        Assert.False(options.User.RequireUniqueEmail);

        Assert.Equal(ClaimTypes.Role, options.ClaimsIdentity.RoleClaimType);
        Assert.Equal(ClaimTypes.Name, options.ClaimsIdentity.UserNameClaimType);
        Assert.Equal(ClaimTypes.NameIdentifier, options.ClaimsIdentity.UserIdClaimType);
        Assert.Equal("AspNet.Identity.SecurityStamp", options.ClaimsIdentity.SecurityStampClaimType);
    }

    [Fact]
    public void CanCustomizeIdentityOptions()
    {
        var services = new ServiceCollection().Configure<IdentityOptions>(options => options.Password.RequiredLength = -1);
        services.AddIdentity<PocoUser, PocoRole>();
        var serviceProvider = services.BuildServiceProvider();

        var setup = serviceProvider.GetRequiredService<IConfigureOptions<IdentityOptions>>();
        Assert.NotNull(setup);
        var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
        Assert.NotNull(optionsGetter);
        var myOptions = optionsGetter.Value;
        Assert.True(myOptions.Password.RequireLowercase);
        Assert.True(myOptions.Password.RequireDigit);
        Assert.True(myOptions.Password.RequireNonAlphanumeric);
        Assert.True(myOptions.Password.RequireUppercase);
        Assert.Equal(1, myOptions.Password.RequiredUniqueChars);
        Assert.Equal(-1, myOptions.Password.RequiredLength);
    }

    [Fact]
    public void CanSetupIdentityOptions()
    {
        var services = new ServiceCollection();
        services.AddIdentity<PocoUser, PocoRole>(options => options.User.RequireUniqueEmail = true);
        var serviceProvider = services.BuildServiceProvider();

        var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
        Assert.NotNull(optionsGetter);

        var myOptions = optionsGetter.Value;
        Assert.True(myOptions.User.RequireUniqueEmail);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PasskeyCookieInheritsTwoFactorConfiguration(int registration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (registration is 0)
        {
            services.AddAuthentication().AddTwoFactorUserIdCookie();
        }
        else if (registration is 1)
        {
            services.AddAuthentication().AddIdentityCookies();
        }
        else
        {
            services.AddIdentity<PocoUser, PocoRole>();
        }
        var sessionStore = new TestTicketStore();
        var cookieManager = new ChunkingCookieManager { ChunkSize = 2048 };
        var timeProvider = new FakeTimeProvider();
        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.Name = "custom-two-factor";
            options.Cookie.Path = "/identity";
            options.Cookie.Domain = "example.com";
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.IsEssential = false;
            options.Cookie.MaxAge = TimeSpan.FromMinutes(10);
            options.Cookie.Extensions.Add("custom=value");
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.TimeProvider = timeProvider;
            options.SessionStore = sessionStore;
            options.CookieManager = cookieManager;
        });
        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        Assert.NotNull(await schemes.GetSchemeAsync(IdentityConstants.PasskeyScheme));
        var monitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var twoFactor = monitor.Get(IdentityConstants.TwoFactorUserIdScheme);
        var passkey = monitor.Get(IdentityConstants.PasskeyScheme);
        Assert.Equal("custom-two-factor.Passkey", passkey.Cookie.Name);
        Assert.Equal("/identity", passkey.Cookie.Path);
        Assert.Equal("example.com", passkey.Cookie.Domain);
        Assert.False(passkey.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, passkey.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, passkey.Cookie.SecurePolicy);
        Assert.False(passkey.Cookie.IsEssential);
        Assert.Equal(TimeSpan.FromMinutes(10), passkey.Cookie.MaxAge);
        Assert.Equal(twoFactor.Cookie.Extensions, passkey.Cookie.Extensions);
        Assert.NotSame(twoFactor.Cookie, passkey.Cookie);
        Assert.Equal(TimeSpan.FromMinutes(30), passkey.ExpireTimeSpan);
        Assert.False(passkey.SlidingExpiration);
        Assert.Same(timeProvider, passkey.TimeProvider);
        Assert.Same(sessionStore, passkey.SessionStore);
        Assert.Same(cookieManager, passkey.CookieManager);
        Assert.NotSame(twoFactor.Events, passkey.Events);
        Assert.NotSame(twoFactor.TicketDataFormat, passkey.TicketDataFormat);
    }

    [Fact]
    public async Task PasskeyCookieConfigurationAfterIdentityOverridesInheritedLifetime()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddIdentityApiEndpoints<PocoUser>();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .ReturnsAsync(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{}",
                AssertionState = "assertion-state",
            });
        services.AddSingleton(passkeyHandler.Object);
        services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = false;
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        signIn.Context = context;
        Assert.Equal("{}", await signIn.MakePasskeyRequestOptionsAsync(null));
        var cookie = context.Response.Headers.SetCookie.ToString().Split(';')[0];
        Assert.StartsWith(IdentityConstants.PasskeyScheme + "=", cookie);
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.Equal(TimeSpan.FromMinutes(5), options.Get(IdentityConstants.TwoFactorUserIdScheme).ExpireTimeSpan);

        timeProvider.Advance(TimeSpan.FromMinutes(6));
        using var nextScope = provider.CreateScope();
        var nextContext = new DefaultHttpContext { RequestServices = nextScope.ServiceProvider };
        nextContext.Request.Headers.Cookie = cookie;
        var result = await nextContext.AuthenticateAsync(IdentityConstants.PasskeyScheme);
        Assert.True(result.Succeeded);
        Assert.Equal("assertion-state", result.Properties.Items["PasskeyState"]);
        Assert.Equal(TimeSpan.FromMinutes(15), result.Properties.ExpiresUtc - result.Properties.IssuedUtc);
        timeProvider.Advance(TimeSpan.FromMinutes(10));
        using var expiredScope = provider.CreateScope();
        var expiredContext = new DefaultHttpContext { RequestServices = expiredScope.ServiceProvider };
        expiredContext.Request.Headers.Cookie = cookie;
        var expiredResult = await expiredContext.AuthenticateAsync(IdentityConstants.PasskeyScheme);
        Assert.False(expiredResult.Succeeded);
        Assert.NotNull(expiredResult.Failure);
    }

    [Theory]
    [InlineData("/app-a")]
    [InlineData("/app-b")]
    public void PasskeyCookieDefaultsToRequestPathBase(string pathBase)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication().AddIdentityCookies();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var passkey = options.Get(IdentityConstants.PasskeyScheme);
        var context = new DefaultHttpContext();
        context.Request.PathBase = pathBase;
        Assert.Equal(pathBase, new CookieAuthenticationOptions().Cookie.Build(context).Path);
        Assert.Null(passkey.Cookie.Path);
        Assert.Equal(pathBase, passkey.Cookie.Build(context).Path);

        passkey.Cookie.Path = "/configured";
        Assert.Equal("/configured", passkey.Cookie.Build(context).Path);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PasskeyCookieUsesConfiguredDataProtectionProvider(bool configurePasskeyProvider, bool configureBeforeRegistration)
    {
        var twoFactorProvider = new EphemeralDataProtectionProvider();
        var passkeyProvider = new EphemeralDataProtectionProvider();
        var expectedProvider = configurePasskeyProvider ? passkeyProvider : twoFactorProvider;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<PocoUser>().AddSignInManager();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .ReturnsAsync(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{}",
                AssertionState = "assertion-state",
            });
        services.AddSingleton(passkeyHandler.Object);
        if (configurePasskeyProvider && configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.DataProtectionProvider = passkeyProvider);
        }
        services.AddAuthentication().AddTwoFactorUserIdCookie()
            .Configure(options => options.DataProtectionProvider = twoFactorProvider);
        if (configurePasskeyProvider && !configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.DataProtectionProvider = passkeyProvider);
        }
        services.AddSingleton<IDataProtectionProvider>(_ => new EphemeralDataProtectionProvider());
        using var firstProvider = services.BuildServiceProvider();
        using var secondProvider = services.BuildServiceProvider();
        Assert.NotSame(firstProvider.GetRequiredService<IDataProtectionProvider>(),
            secondProvider.GetRequiredService<IDataProtectionProvider>());
        using var firstScope = firstProvider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = firstScope.ServiceProvider };
        var signIn = firstScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        signIn.Context = context;

        Assert.Equal("{}", await signIn.MakePasskeyRequestOptionsAsync(null));
        var cookie = context.Response.Headers.SetCookie.ToString().Split(';')[0];
        Assert.StartsWith(IdentityConstants.PasskeyScheme + "=", cookie);

        using var secondScope = secondProvider.CreateScope();
        var nextContext = new DefaultHttpContext { RequestServices = secondScope.ServiceProvider };
        nextContext.Request.Headers.Cookie = cookie;
        var result = await nextContext.AuthenticateAsync(IdentityConstants.PasskeyScheme);
        Assert.True(result.Succeeded);
        Assert.Equal("assertion-state", result.Properties.Items["PasskeyState"]);

        var monitor = secondProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var passkey = monitor.Get(IdentityConstants.PasskeyScheme);
        var twoFactor = monitor.Get(IdentityConstants.TwoFactorUserIdScheme);
        Assert.Same(expectedProvider, passkey.DataProtectionProvider);
        var cookieValue = cookie[(cookie.IndexOf('=') + 1)..];
        var passkeyFormat = new TicketDataFormat(expectedProvider.CreateProtector(
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", IdentityConstants.PasskeyScheme, "v2"));
        Assert.Equal("assertion-state", passkeyFormat.Unprotect(cookieValue).Properties.Items["PasskeyState"]);
        Assert.Null(twoFactor.TicketDataFormat.Unprotect(cookieValue));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PasskeyCookieUsesConfiguredSessionStore(bool configurePasskeyStore, bool configureBeforeRegistration)
    {
        var twoFactorStore = new TestTicketStore();
        var passkeyStore = new TestTicketStore();
        var expectedStore = configurePasskeyStore ? passkeyStore : twoFactorStore;
        var otherStore = configurePasskeyStore ? twoFactorStore : passkeyStore;
        var largeAssertionState = new string('a', 8192);
        var passkey = new UserPasskeyInfo(
            credentialId: [1, 2, 3], publicKey: [4, 5, 6], createdAt: DateTimeOffset.UtcNow,
            signCount: 0, transports: [], isUserVerified: true, isBackupEligible: true,
            isBackedUp: true, attestationObject: [7, 8, 9], clientDataJson: [10, 11, 12]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<PocoUser>().AddSignInManager();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .ReturnsAsync(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{}",
                AssertionState = largeAssertionState,
            });
        passkeyHandler.Setup(handler => handler.PerformAssertionAsync(
                It.Is<PasskeyAssertionContext>(context => context.AssertionState == largeAssertionState)))
            .ReturnsAsync(PasskeyAssertionResult.Success(passkey, new PocoUser { UserName = "hao" }))
            .Verifiable();
        services.AddSingleton(passkeyHandler.Object);
        if (configurePasskeyStore && configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.SessionStore = passkeyStore);
        }
        services.AddAuthentication().AddTwoFactorUserIdCookie()
            .Configure(options => options.SessionStore = twoFactorStore);
        if (configurePasskeyStore && !configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.SessionStore = passkeyStore);
        }

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.Same(expectedStore, monitor.Get(IdentityConstants.PasskeyScheme).SessionStore);

        using var firstScope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = firstScope.ServiceProvider };
        var signIn = firstScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        signIn.Context = context;

        Assert.Equal("{}", await signIn.MakePasskeyRequestOptionsAsync(null));
        var setCookies = context.Response.Headers.SetCookie.ToArray();
        var setCookie = Assert.Single(setCookies);
        var cookie = setCookie.Split(';')[0];
        Assert.StartsWith(IdentityConstants.PasskeyScheme + "=", cookie);
        Assert.True(cookie.Length < 1024);
        Assert.Equal(1, expectedStore.StoreCount);
        Assert.Equal(1, expectedStore.Count);
        Assert.Equal(0, otherStore.StoreCount);

        using var secondScope = provider.CreateScope();
        var nextContext = new DefaultHttpContext { RequestServices = secondScope.ServiceProvider };
        nextContext.Request.Headers.Cookie = cookie;
        var nextSignIn = secondScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        nextSignIn.Context = nextContext;

        var assertionResult = await nextSignIn.PerformPasskeyAssertionAsync("{}");
        Assert.True(assertionResult.Succeeded);
        Assert.Same(passkey, assertionResult.Passkey);
        passkeyHandler.Verify();
        Assert.Equal(1, expectedStore.RetrieveCount);
        Assert.Equal(1, expectedStore.RemoveCount);
        Assert.Equal(0, expectedStore.Count);
        Assert.Equal(0, otherStore.RetrieveCount);
        Assert.Equal(0, otherStore.RemoveCount);
        Assert.Contains(nextContext.Response.Headers.SetCookie, header => header.StartsWith(IdentityConstants.PasskeyScheme + "=;", StringComparison.Ordinal));

        using var replayScope = provider.CreateScope();
        var replayContext = new DefaultHttpContext { RequestServices = replayScope.ServiceProvider };
        replayContext.Request.Headers.Cookie = cookie;
        var replayAuth = await replayContext.AuthenticateAsync(IdentityConstants.PasskeyScheme);
        Assert.False(replayAuth.Succeeded);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PasskeyCookieUsesConfiguredCookieManager(bool configurePasskeyManager, bool configureBeforeRegistration)
    {
        var twoFactorManager = new ChunkingCookieManager { ChunkSize = 1500 };
        var passkeyManager = new ChunkingCookieManager { ChunkSize = 900 };
        var expectedManager = configurePasskeyManager ? passkeyManager : twoFactorManager;
        var assertionState = new string('a', 2000);
        var passkey = new UserPasskeyInfo(
            credentialId: [1, 2, 3], publicKey: [4, 5, 6], createdAt: DateTimeOffset.UtcNow,
            signCount: 0, transports: [], isUserVerified: true, isBackupEligible: true,
            isBackedUp: true, attestationObject: [7, 8, 9], clientDataJson: [10, 11, 12]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<PocoUser>().AddSignInManager();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .ReturnsAsync(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{}",
                AssertionState = assertionState,
            });
        passkeyHandler.Setup(handler => handler.PerformAssertionAsync(
                It.Is<PasskeyAssertionContext>(context => context.AssertionState == assertionState)))
            .ReturnsAsync(PasskeyAssertionResult.Success(passkey, new PocoUser { UserName = "hao" }))
            .Verifiable();
        services.AddSingleton(passkeyHandler.Object);
        if (configurePasskeyManager && configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.CookieManager = passkeyManager);
        }
        services.AddAuthentication().AddTwoFactorUserIdCookie()
            .Configure(options => options.CookieManager = twoFactorManager);
        if (configurePasskeyManager && !configureBeforeRegistration)
        {
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme,
                options => options.CookieManager = passkeyManager);
        }

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.Same(expectedManager, monitor.Get(IdentityConstants.PasskeyScheme).CookieManager);

        using var firstScope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = firstScope.ServiceProvider };
        var signIn = firstScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        signIn.Context = context;

        Assert.Equal("{}", await signIn.MakePasskeyRequestOptionsAsync(null));
        var setCookies = context.Response.Headers.SetCookie.ToArray();
        Assert.True(setCookies.Length > 2);
        Assert.Contains(setCookies, header => header.StartsWith(IdentityConstants.PasskeyScheme + "=chunks-", StringComparison.Ordinal));
        Assert.All(setCookies, header => Assert.True(header.Length <= expectedManager.ChunkSize));
        var cookieHeader = string.Join("; ", setCookies.Select(header => header.Split(';')[0]));

        using var secondScope = provider.CreateScope();
        var nextContext = new DefaultHttpContext { RequestServices = secondScope.ServiceProvider };
        nextContext.Request.Headers.Cookie = cookieHeader;
        var nextSignIn = secondScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        nextSignIn.Context = nextContext;

        var assertionResult = await nextSignIn.PerformPasskeyAssertionAsync("{}");
        Assert.True(assertionResult.Succeeded);
        Assert.Same(passkey, assertionResult.Passkey);
        passkeyHandler.Verify();
        var deleteCookies = nextContext.Response.Headers.SetCookie.ToArray();
        Assert.Equal(setCookies.Length, deleteCookies.Length);
        Assert.Contains(deleteCookies, header => header.StartsWith(IdentityConstants.PasskeyScheme + "=;", StringComparison.Ordinal));
        Assert.Contains(deleteCookies, header => header.StartsWith(IdentityConstants.PasskeyScheme + "C1=;", StringComparison.Ordinal));
    }

    private sealed class TestTicketStore : ITicketStore
    {
        private readonly ConcurrentDictionary<string, AuthenticationTicket> _tickets = new();
        private int _storeCount;
        private int _retrieveCount;
        private int _removeCount;

        public int Count => _tickets.Count;
        public int StoreCount => Volatile.Read(ref _storeCount);
        public int RetrieveCount => Volatile.Read(ref _retrieveCount);
        public int RemoveCount => Volatile.Read(ref _removeCount);

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            Interlocked.Increment(ref _storeCount);
            var key = Guid.NewGuid().ToString("N");
            _tickets[key] = ticket;
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            _tickets[key] = ticket;
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            Interlocked.Increment(ref _retrieveCount);
            _tickets.TryGetValue(key, out var ticket);
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key)
        {
            Interlocked.Increment(ref _removeCount);
            _tickets.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PasskeyOptionsCanBeCreatedWithStandaloneCookieRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<PocoUser>().AddSignInManager();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .ReturnsAsync(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{}",
                AssertionState = "assertion-state",
            });
        services.AddSingleton(passkeyHandler.Object);
        services.AddAuthentication().AddTwoFactorUserIdCookie();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        signIn.Context = context;

        Assert.Equal("{}", await signIn.MakePasskeyRequestOptionsAsync(null));
        var cookie = context.Response.Headers.SetCookie.ToString();
        Assert.StartsWith(IdentityConstants.PasskeyScheme + "=", cookie);
        Assert.DoesNotContain(IdentityConstants.TwoFactorUserIdScheme + "=", cookie);

        using var nextScope = provider.CreateScope();
        var nextContext = new DefaultHttpContext { RequestServices = nextScope.ServiceProvider };
        nextContext.Request.Headers.Cookie = cookie.Split(';')[0];
        var result = await nextContext.AuthenticateAsync(IdentityConstants.PasskeyScheme);
        Assert.True(result.Succeeded);
        Assert.Equal("assertion-state", result.Properties.Items["PasskeyState"]);
    }

    [Fact]
    public async Task StandaloneTwoFactorCookieCanAuthenticateWithExplicitDefaultScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(IdentityConstants.TwoFactorUserIdScheme).AddTwoFactorUserIdCookie();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        var result = await context.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public void CanConfigureCookieOptions()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddIdentityCookies(o =>
        {
            o.ApplicationCookie.Configure(a => a.Cookie.Name = "a");
            o.ExternalCookie.Configure(a => a.Cookie.Name = "b");
            o.TwoFactorRememberMeCookie.Configure(a => a.Cookie.Name = "c");
            o.TwoFactorUserIdCookie.Configure(a => a.Cookie.Name = "d");
        });
        services.Configure<CookieAuthenticationOptions>("Identity.Passkey", a => a.Cookie.Name = "e");
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.NotNull(options);

        Assert.Equal("a", options.Get(IdentityConstants.ApplicationScheme).Cookie.Name);
        Assert.Equal("b", options.Get(IdentityConstants.ExternalScheme).Cookie.Name);
        Assert.Equal("c", options.Get(IdentityConstants.TwoFactorRememberMeScheme).Cookie.Name);
        Assert.Equal("d", options.Get(IdentityConstants.TwoFactorUserIdScheme).Cookie.Name);
        Assert.Equal("e", options.Get(IdentityConstants.PasskeyScheme).Cookie.Name);
    }

    [Fact]
    public async Task PasskeyCookieSupportsPerTenantSchemeForwardingWithoutSharingTwoFactorTargets()
    {
        var passkey = new UserPasskeyInfo(
            credentialId: [1, 2, 3], publicKey: [4, 5, 6], createdAt: DateTimeOffset.UtcNow,
            signCount: 0, transports: [], isUserVerified: true, isBackupEligible: true,
            isBackedUp: true, attestationObject: [7, 8, 9], clientDataJson: [10, 11, 12]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<PocoUser>().AddSignInManager();
        services.AddSingleton(Mock.Of<IUserStore<PocoUser>>());
        var passkeyHandler = new Mock<IPasskeyHandler<PocoUser>>();
        passkeyHandler.Setup(handler => handler.MakeRequestOptionsAsync(null, It.IsAny<HttpContext>()))
            .Returns((PocoUser _, HttpContext httpContext) => Task.FromResult(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = httpContext.Request.Path.StartsWithSegments("/tenant-b") ? "{\"tenant\":\"b\"}" : "{\"tenant\":\"a\"}",
                AssertionState = httpContext.Request.Path.StartsWithSegments("/tenant-b") ? "state-b" : "state-a",
            }));
        passkeyHandler.Setup(handler => handler.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()))
            .Returns((PasskeyAssertionContext context) =>
            {
                var expectedState = context.HttpContext.Request.Path.StartsWithSegments("/tenant-b") ? "state-b" : "state-a";
                Assert.Equal(expectedState, context.AssertionState);
                return Task.FromResult(PasskeyAssertionResult.Success(passkey, new PocoUser { UserName = expectedState }));
            });
        services.AddSingleton(passkeyHandler.Object);

        var authBuilder = services.AddAuthentication();
        authBuilder.AddCookie("TenantA.TwoFactor", o =>
        {
            o.Cookie.Name = "TenantA.TwoFactor";
            o.Cookie.Path = "/tenant-a";
        });
        authBuilder.AddCookie("TenantB.TwoFactor", o =>
        {
            o.Cookie.Name = "TenantB.TwoFactor";
            o.Cookie.Path = "/tenant-b";
        });
        authBuilder.AddCookie("TenantA.Passkey", o =>
        {
            o.Cookie.Name = "TenantA.Passkey";
            o.Cookie.Path = "/tenant-a";
        });
        authBuilder.AddCookie("TenantB.Passkey", o =>
        {
            o.Cookie.Name = "TenantB.Passkey";
            o.Cookie.Path = "/tenant-b";
        });
        authBuilder.AddTwoFactorUserIdCookie()
            .Configure(options =>
            {
                options.ForwardDefault = "TenantA.TwoFactor";
                options.ForwardAuthenticate = "TenantA.TwoFactor";
                options.ForwardChallenge = "TenantA.TwoFactor";
                options.ForwardForbid = "TenantA.TwoFactor";
                options.ForwardSignIn = "TenantA.TwoFactor";
                options.ForwardSignOut = "TenantA.TwoFactor";
                options.ForwardDefaultSelector = ctx => ctx.Request.Path.StartsWithSegments("/tenant-b")
                    ? "TenantB.TwoFactor"
                    : "TenantA.TwoFactor";
            });

        using (var unforwardedProvider = services.BuildServiceProvider())
        {
            var unforwardedPasskey = unforwardedProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(IdentityConstants.PasskeyScheme);
            Assert.Null(unforwardedPasskey.ForwardDefault);
            Assert.Null(unforwardedPasskey.ForwardAuthenticate);
            Assert.Null(unforwardedPasskey.ForwardChallenge);
            Assert.Null(unforwardedPasskey.ForwardForbid);
            Assert.Null(unforwardedPasskey.ForwardSignIn);
            Assert.Null(unforwardedPasskey.ForwardSignOut);
            Assert.Null(unforwardedPasskey.ForwardDefaultSelector);
        }

        services.Configure<CookieAuthenticationOptions>(IdentityConstants.PasskeyScheme, options =>
        {
            options.ForwardDefaultSelector = ctx => ctx.Request.Path.StartsWithSegments("/tenant-b")
                ? "TenantB.Passkey"
                : "TenantA.Passkey";
        });

        using var provider = services.BuildServiceProvider();

        using var tenantAStartScope = provider.CreateScope();
        var tenantAStartContext = new DefaultHttpContext { RequestServices = tenantAStartScope.ServiceProvider };
        tenantAStartContext.Request.Path = "/tenant-a/passkey";
        var tenantASignIn = tenantAStartScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        tenantASignIn.Context = tenantAStartContext;
        Assert.Equal("{\"tenant\":\"a\"}", await tenantASignIn.MakePasskeyRequestOptionsAsync(null));
        var tenantAPasskeySetCookie = Assert.Single(tenantAStartContext.Response.Headers.SetCookie);
        Assert.StartsWith("TenantA.Passkey=", tenantAPasskeySetCookie);
        Assert.Contains("path=/tenant-a", tenantAPasskeySetCookie, StringComparison.OrdinalIgnoreCase);

        await tenantAStartContext.SignInAsync(
            IdentityConstants.TwoFactorUserIdScheme,
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "tenant-a-user")], IdentityConstants.TwoFactorUserIdScheme)));
        var tenantATwoFactorSetCookie = Assert.Single(tenantAStartContext.Response.Headers.SetCookie,
            header => header.StartsWith("TenantA.TwoFactor=", StringComparison.Ordinal));
        var tenantAPasskeyCookie = tenantAPasskeySetCookie.Split(';')[0];
        var tenantATwoFactorCookie = tenantATwoFactorSetCookie.Split(';')[0];

        using var tenantBStartScope = provider.CreateScope();
        var tenantBStartContext = new DefaultHttpContext { RequestServices = tenantBStartScope.ServiceProvider };
        tenantBStartContext.Request.Path = "/tenant-b/passkey";
        var tenantBSignIn = tenantBStartScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        tenantBSignIn.Context = tenantBStartContext;
        Assert.Equal("{\"tenant\":\"b\"}", await tenantBSignIn.MakePasskeyRequestOptionsAsync(null));
        var tenantBPasskeySetCookie = Assert.Single(tenantBStartContext.Response.Headers.SetCookie);
        Assert.StartsWith("TenantB.Passkey=", tenantBPasskeySetCookie);
        Assert.Contains("path=/tenant-b", tenantBPasskeySetCookie, StringComparison.OrdinalIgnoreCase);
        var tenantBPasskeyCookie = tenantBPasskeySetCookie.Split(';')[0];

        using var tenantACompleteScope = provider.CreateScope();
        var tenantACompleteContext = new DefaultHttpContext { RequestServices = tenantACompleteScope.ServiceProvider };
        tenantACompleteContext.Request.Path = "/tenant-a/passkey";
        tenantACompleteContext.Request.Headers.Cookie = $"{tenantAPasskeyCookie}; {tenantATwoFactorCookie}";
        var tenantACompleteSignIn = tenantACompleteScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        tenantACompleteSignIn.Context = tenantACompleteContext;
        var tenantAResult = await tenantACompleteSignIn.PerformPasskeyAssertionAsync("{}");
        Assert.True(tenantAResult.Succeeded);
        Assert.Equal("state-a", tenantAResult.User.UserName);
        Assert.Contains(tenantACompleteContext.Response.Headers.SetCookie,
            header => header.StartsWith("TenantA.Passkey=;", StringComparison.Ordinal));
        var tenantATwoFactorAuth = await tenantACompleteContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        Assert.True(tenantATwoFactorAuth.Succeeded);
        Assert.Equal("tenant-a-user", tenantATwoFactorAuth.Principal.Identity.Name);

        using var tenantBCompleteScope = provider.CreateScope();
        var tenantBCompleteContext = new DefaultHttpContext { RequestServices = tenantBCompleteScope.ServiceProvider };
        tenantBCompleteContext.Request.Path = "/tenant-b/passkey";
        tenantBCompleteContext.Request.Headers.Cookie = tenantBPasskeyCookie;
        var tenantBCompleteSignIn = tenantBCompleteScope.ServiceProvider.GetRequiredService<SignInManager<PocoUser>>();
        tenantBCompleteSignIn.Context = tenantBCompleteContext;
        var tenantBResult = await tenantBCompleteSignIn.PerformPasskeyAssertionAsync("{}");
        Assert.True(tenantBResult.Succeeded);
        Assert.Equal("state-b", tenantBResult.User.UserName);
        Assert.Contains(tenantBCompleteContext.Response.Headers.SetCookie,
            header => header.StartsWith("TenantB.Passkey=;", StringComparison.Ordinal));
    }
}
