// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Security;

// UserManager and SignInManager are scoped, so they are constructed per request.
[MemoryDiagnoser]
public class IdentityManagerResolveBenchmark
{
    private ServiceProvider _services;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddAuthentication();
        services.AddScoped<IUserStore<BenchmarkUser>, BenchmarkUserStore>();
        services.AddIdentityCore<BenchmarkUser>().AddSignInManager().AddDefaultTokenProviders();
        _services = services.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup() => _services.Dispose();

    [Benchmark]
    public object ResolveUserManager()
    {
        using var scope = _services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<UserManager<BenchmarkUser>>();
    }

    [Benchmark]
    public object ResolveSignInManager()
    {
        using var scope = _services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SignInManager<BenchmarkUser>>();
    }

    public sealed class BenchmarkUser
    {
    }

    private sealed class BenchmarkUserStore : IUserStore<BenchmarkUser>
    {
        public Task<IdentityResult> CreateAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> DeleteAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<BenchmarkUser> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<BenchmarkUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetNormalizedUserNameAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetUserIdAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetUserNameAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetNormalizedUserNameAsync(BenchmarkUser user, string normalizedName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetUserNameAsync(BenchmarkUser user, string userName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> UpdateAsync(BenchmarkUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Dispose()
        {
        }
    }
}
