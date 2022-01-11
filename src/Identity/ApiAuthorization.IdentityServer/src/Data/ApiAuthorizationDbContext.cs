// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

/// <summary>
/// Database abstraction for a combined <see cref="DbContext"/> using ASP.NET Identity and Identity Server.
/// </summary>
/// <typeparam name="TUser"></typeparam>
public class ApiAuthorizationDbContext<TUser> : IdentityDbContext<TUser>, IPersistedGrantDbContext where TUser : IdentityUser
{
    private readonly IOptions<OperationalStoreOptions> _operationalStoreOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="ApiAuthorizationDbContext{TUser}"/>.
    /// </summary>
    /// <param name="options">The <see cref="DbContextOptions"/>.</param>
    /// <param name="operationalStoreOptions">The <see cref="IOptions{OperationalStoreOptions}"/>.</param>
    public ApiAuthorizationDbContext(
        DbContextOptions options,
        IOptions<OperationalStoreOptions> operationalStoreOptions)
        : base(options)
    {
        _operationalStoreOptions = operationalStoreOptions;
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{PersistedGrant}"/>.
    /// </summary>
    public DbSet<PersistedGrant> PersistedGrants { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{DeviceFlowCodes}"/>.
    /// </summary>
    public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{Key}"/>.
    /// </summary>
    public DbSet<Key> Keys { get; set; }

    Task<int> IPersistedGrantDbContext.SaveChangesAsync() => base.SaveChangesAsync();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigurePersistedGrantContext(_operationalStoreOptions.Value);
    }
}
