// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class DataProtectorTokenProviderTest
{
    private static DataProtectorTokenProvider<PocoUser> CreateProvider(DataProtectionTokenProviderOptions options = null)
    {
        var dataProtectionProvider = new EphemeralDataProtectionProvider(new LoggerFactory());
        var optionsAccessor = new Mock<IOptions<DataProtectionTokenProviderOptions>>();
        optionsAccessor.Setup(o => o.Value).Returns(options ?? new DataProtectionTokenProviderOptions());
        var logger = new Mock<ILogger<DataProtectorTokenProvider<PocoUser>>>().Object;
        return new DataProtectorTokenProvider<PocoUser>(dataProtectionProvider, optionsAccessor.Object, logger);
    }

    private static UserManager<PocoUser> CreateUserManager()
        => MockHelpers.TestUserManager(new NoopUserStore());

    [Fact]
    public async Task GenerateAndValidateTokenSucceeds()
    {
        var provider = CreateProvider();
        var manager = CreateUserManager();
        var user = new PocoUser("testuser");

        var token = await provider.GenerateAsync("purpose", manager, user);
        var valid = await provider.ValidateAsync("purpose", token, manager, user);

        Assert.True(valid);
    }

    [Fact]
    public async Task ValidateFailsForDifferentPurpose()
    {
        var provider = CreateProvider();
        var manager = CreateUserManager();
        var user = new PocoUser("testuser");

        var token = await provider.GenerateAsync("purpose1", manager, user);
        var valid = await provider.ValidateAsync("purpose2", token, manager, user);

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateFailsForDifferentUser()
    {
        var provider = CreateProvider();
        var manager = CreateUserManager();
        var user1 = new PocoUser("user1");
        var user2 = new PocoUser("user2");

        var token = await provider.GenerateAsync("purpose", manager, user1);
        var valid = await provider.ValidateAsync("purpose", token, manager, user2);

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateFailsAfterTokenLifespanExpires()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new DataProtectionTokenProviderOptions
        {
            TokenLifespan = TimeSpan.FromHours(1),
            TimeProvider = timeProvider,
        };
        var provider = CreateProvider(options);
        var manager = CreateUserManager();
        var user = new PocoUser("testuser");

        var token = await provider.GenerateAsync("purpose", manager, user);

        timeProvider.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));

        var valid = await provider.ValidateAsync("purpose", token, manager, user);

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateSucceedsWithinTokenLifespan()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new DataProtectionTokenProviderOptions
        {
            TokenLifespan = TimeSpan.FromHours(1),
            TimeProvider = timeProvider,
        };
        var provider = CreateProvider(options);
        var manager = CreateUserManager();
        var user = new PocoUser("testuser");

        var token = await provider.GenerateAsync("purpose", manager, user);

        timeProvider.Advance(TimeSpan.FromMinutes(59));

        var valid = await provider.ValidateAsync("purpose", token, manager, user);

        Assert.True(valid);
    }

    [Fact]
    public async Task TimeProviderFromDIIsInjectedViaPostConfigure()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider(new LoggerFactory()));
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddIdentityCore<PocoUser>()
            .AddDefaultTokenProviders()
            .AddUserStore<NoopUserStore>();

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<UserManager<PocoUser>>();
        var user = new PocoUser("testuser");

        var token = await manager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "purpose");

        timeProvider.Advance(TimeSpan.FromDays(1) + TimeSpan.FromSeconds(1));

        var valid = await manager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "purpose", token);

        Assert.False(valid);
    }

    [Fact]
    public async Task TimeProviderFromDIDoesNotOverrideManuallySetTimeProvider()
    {
        var diTimeProvider = new FakeTimeProvider();
        var optionsTimeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider(new LoggerFactory()));
        services.AddSingleton<TimeProvider>(diTimeProvider);
        services.AddIdentityCore<PocoUser>()
            .AddDefaultTokenProviders()
            .AddUserStore<NoopUserStore>();
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TimeProvider = optionsTimeProvider);

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<UserManager<PocoUser>>();
        var user = new PocoUser("testuser");

        var token = await manager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "purpose");

        // Advance only the options time provider — should cause expiry
        optionsTimeProvider.Advance(TimeSpan.FromDays(1) + TimeSpan.FromSeconds(1));

        var valid = await manager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "purpose", token);

        Assert.False(valid);
    }
}
