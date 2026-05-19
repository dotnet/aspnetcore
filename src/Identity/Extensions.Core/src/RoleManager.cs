// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Identity.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides the APIs for managing roles in a persistence store.
/// </summary>
/// <typeparam name="TRole">The type encapsulating a role.</typeparam>
public class RoleManager<TRole> : IDisposable where TRole : class
{
    private bool _disposed;

    /// <summary>
    /// The cancellation token used to cancel operations.
    /// </summary>
    protected virtual CancellationToken CancellationToken => CancellationToken.None;

    /// <summary>
    /// Constructs a new instance of <see cref="RoleManager{TRole}"/>.
    /// </summary>
    /// <param name="store">The persistence store the manager will operate over.</param>
    /// <param name="roleValidators">A collection of validators for roles.</param>
    /// <param name="keyNormalizer">The normalizer to use when normalizing role names to keys.</param>
    /// <param name="errors">The <see cref="IdentityErrorDescriber"/> used to provider error messages.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    public RoleManager(IRoleStore<TRole> store,
        IEnumerable<IRoleValidator<TRole>> roleValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        ILogger<RoleManager<TRole>> logger)
    {
        ArgumentNullThrowHelper.ThrowIfNull(store);
        Store = store;
        KeyNormalizer = keyNormalizer;
        ErrorDescriber = errors;
        Logger = logger;

        if (roleValidators != null)
        {
            foreach (var v in roleValidators)
            {
                RoleValidators.Add(v);
            }
        }
    }

    /// <summary>
    /// Gets the persistence store this instance operates over.
    /// </summary>
    /// <value>The persistence store this instance operates over.</value>
    protected IRoleStore<TRole> Store { get; private set; }

    /// <summary>
    /// Gets the <see cref="ILogger"/> used to log messages from the manager.
    /// </summary>
    /// <value>
    /// The <see cref="ILogger"/> used to log messages from the manager.
    /// </value>
    public virtual ILogger Logger { get; set; }

    /// <summary>
    /// Gets a list of validators for roles to call before persistence.
    /// </summary>
    /// <value>A list of validators for roles to call before persistence.</value>
    public IList<IRoleValidator<TRole>> RoleValidators { get; } = new List<IRoleValidator<TRole>>();

    /// <summary>
    /// Gets the <see cref="IdentityErrorDescriber"/> used to provider error messages.
    /// </summary>
    /// <value>
    /// The <see cref="IdentityErrorDescriber"/> used to provider error messages.
    /// </value>
    public IdentityErrorDescriber ErrorDescriber { get; set; }

    /// <summary>
    /// Gets the normalizer to use when normalizing role names to keys.
    /// </summary>
    /// <value>
    /// The normalizer to use when normalizing role names to keys.
    /// </value>
    public ILookupNormalizer KeyNormalizer { get; set; }

    /// <summary>
    /// Gets an IQueryable collection of Roles if the persistence store is an <see cref="IQueryableRoleStore{TRole}"/>,
    /// otherwise throws a <see cref="NotSupportedException"/>.
    /// </summary>
    /// <value>An IQueryable collection of Roles if the persistence store is an <see cref="IQueryableRoleStore{TRole}"/>.</value>
    /// <exception cref="NotSupportedException">Thrown if the persistence store is not an <see cref="IQueryableRoleStore{TRole}"/>.</exception>
    /// <remarks>
    /// Callers to this property should use <see cref="SupportsQueryableRoles"/> to ensure the backing role store supports
    /// returning an IQueryable list of roles.
    /// </remarks>
    public virtual IQueryable<TRole> Roles
    {
        get
        {
            var queryableStore = Store as IQueryableRoleStore<TRole>;
            if (queryableStore == null)
            {
                throw new NotSupportedException(Resources.StoreNotIQueryableRoleStore);
            }
            return queryableStore.Roles;
        }
    }

    /// <summary>
    /// Gets a flag indicating whether the underlying persistence store supports returning an <see cref="IQueryable"/> collection of roles.
    /// </summary>
    /// <value>
    /// true if the underlying persistence store supports returning an <see cref="IQueryable"/> collection of roles, otherwise false.
    /// </value>
    public virtual bool SupportsQueryableRoles
    {
        get
        {
            ThrowIfDisposed();
            return Store is IQueryableRoleStore<TRole>;
        }
    }

    /// <summary>
    /// Gets a flag indicating whether the underlying persistence store supports <see cref="Claim"/>s for roles.
    /// </summary>
    /// <value>
    /// true if the underlying persistence store supports <see cref="Claim"/>s for roles, otherwise false.
    /// </value>
    public virtual bool SupportsRoleClaims
    {
        get
        {
            ThrowIfDisposed();
            return Store is IRoleClaimStore<TRole>;
        }
    }

    /// <summary>
    /// Creates the specified <paramref name="role"/> in the persistence store.
    /// </summary>
    /// <param name="role">The role to create.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    public virtual async Task<IdentityResult> CreateAsync(TRole role)
    {
        ThrowIfDisposed();
        ArgumentNullThrowHelper.ThrowIfNull(role);
        var result = await ValidateRoleAsync(role).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result;
        }
        await UpdateNormalizedRoleNameAsync(role).ConfigureAwait(false);
        result = await Store.CreateAsync(role, CancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Updates the normalized name for the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role whose normalized name needs to be updated.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    public virtual async Task UpdateNormalizedRoleNameAsync(TRole role)
    {
        var name = await GetRoleNameAsync(role).ConfigureAwait(false);
        await Store.SetNormalizedRoleNameAsync(role, NormalizeKey(name), CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role to updated.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> for the update.
    /// </returns>
    public virtual Task<IdentityResult> UpdateAsync(TRole role)
    {
        ThrowIfDisposed();
        ArgumentNullThrowHelper.ThrowIfNull(role);

        return UpdateRoleAsync(role);
    }

    /// <summary>
    /// Deletes the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role to delete.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> for the delete.
    /// </returns>
    public virtual Task<IdentityResult> DeleteAsync(TRole role)
    {
        ThrowIfDisposed();
        ArgumentNullThrowHelper.ThrowIfNull(role);

        return Store.DeleteAsync(role, CancellationToken);
    }

    /// <summary>
    /// Gets a flag indicating whether the specified <paramref name="roleName"/> exists.
    /// </summary>
    /// <param name="roleName">The role name whose existence should be checked.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing true if the role name exists, otherwise false.
    /// </returns>
    public virtual async Task<bool> RoleExistsAsync(string roleName)
    {
        ThrowIfDisposed();
        ArgumentNullThrowHelper.ThrowIfNull(roleName);

        return await FindByNameAsync(roleName).ConfigureAwait(false) != null;
    }

    /// <summary>
    /// Gets a normalized representation of the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The value to normalize.</param>
    /// <returns>A normalized representation of the specified <paramref name="key"/>.</returns>
    [return: NotNullIfNotNull("key")]
    public virtual string? NormalizeKey(string? key)
    {
        return (KeyNormalizer == null) ? key : KeyNormalizer.NormalizeName(key);
    }

    /// <summary>
    /// Finds the role associated with the specified <paramref name="roleId"/> if any.
    /// </summary>
    /// <param name="roleId">The role ID whose role should be returned.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the role
    /// associated with the specified <paramref name="roleId"/>
    /// </returns>
    public virtual Task<TRole?> FindByIdAsync(string roleId)
    {
        ThrowIfDisposed();
        return Store.FindByIdAsync(roleId, CancellationToken);
    }

    /// <summary>
    /// Gets the name of the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role whose name should be retrieved.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the name of the
    /// specified <paramref name="role"/>.
    /// </returns>
    public virtual Task<string?> GetRoleNameAsync(TRole role)
    {
        ThrowIfDisposed();
        return Store.GetRoleNameAsync(role, CancellationToken);
    }

    /// <summary>
    /// Sets the name of the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role whose name should be set.</param>
    /// <param name="name">The name to set.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
    /// of the operation.
    /// </returns>
    public virtual async Task<IdentityResult> SetRoleNameAsync(TRole role, string? name)
    {
        ThrowIfDisposed();

        await Store.SetRoleNameAsync(role, name, CancellationToken).ConfigureAwait(false);
        await UpdateNormalizedRoleNameAsync(role).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    /// <summary>
    /// Gets the ID of the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role whose ID should be retrieved.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the ID of the
    /// specified <paramref name="role"/>.
    /// </returns>
    public virtual Task<string> GetRoleIdAsync(TRole role)
    {
        ThrowIfDisposed();
        return Store.GetRoleIdAsync(role, CancellationToken);
    }

    /// <summary>
    /// Finds the role associated with the specified <paramref name="roleName"/> if any.
    /// </summary>
    /// <param name="roleName">The name of the role to be returned.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the role
    /// associated with the specified <paramref name="roleName"/>
    /// </returns>
    public virtual Task<TRole?> FindByNameAsync(string roleName)
    {
        ThrowIfDisposed();
        ArgumentNullThrowHelper.ThrowIfNull(roleName);

        return Store.FindByNameAsync(NormalizeKey(roleName), CancellationToken);
    }

    /// <summary>
    /// Adds a claim to a role.
    /// </summary>
    /// <param name="role">The role to add the claim to.</param>
    /// <param name="claim">The claim to add.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
    /// of the operation.
    /// </returns>
    public virtual async Task<IdentityResult> AddClaimAsync(TRole role, Claim claim)
    {
        ThrowIfDisposed();
        var claimStore = GetClaimStore();
        ArgumentNullThrowHelper.ThrowIfNull(claim);
        ArgumentNullThrowHelper.ThrowIfNull(role);

        await claimStore.AddClaimAsync(role, claim, CancellationToken).ConfigureAwait(false);
        return await UpdateRoleAsync(role).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a claim from a role.
    /// </summary>
    /// <param name="role">The role to remove the claim from.</param>
    /// <param name="claim">The claim to remove.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
    /// of the operation.
    /// </returns>
    public virtual async Task<IdentityResult> RemoveClaimAsync(TRole role, Claim claim)
    {
        ThrowIfDisposed();
        var claimStore = GetClaimStore();
        ArgumentNullThrowHelper.ThrowIfNull(role);

        await claimStore.RemoveClaimAsync(role, claim, CancellationToken).ConfigureAwait(false);
        return await UpdateRoleAsync(role).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a list of claims associated with the specified <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role whose claims should be returned.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the list of <see cref="Claim"/>s
    /// associated with the specified <paramref name="role"/>.
    /// </returns>
    public virtual Task<IList<Claim>> GetClaimsAsync(TRole role)
    {
        ThrowIfDisposed();
        var claimStore = GetClaimStore();
        ArgumentNullThrowHelper.ThrowIfNull(role);
        return claimStore.GetClaimsAsync(role, CancellationToken);
    }

    /// <summary>
    /// Releases all resources used by the role manager.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the role manager and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            Store.Dispose();
        }
        _disposed = true;
    }

    /// <summary>
    /// Should return <see cref="IdentityResult.Success"/> if validation is successful. This is
    /// called before saving the role via Create or Update.
    /// </summary>
    /// <param name="role">The role</param>
    /// <returns>A <see cref="IdentityResult"/> representing whether validation was successful.</returns>
    protected virtual async Task<IdentityResult> ValidateRoleAsync(TRole role)
    {
        List<IdentityError>? errors = null;
        foreach (var v in RoleValidators)
        {
            var result = await v.ValidateAsync(this, role).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                errors ??= new List<IdentityError>();
                errors.AddRange(result.Errors);
            }
        }
        if (errors?.Count > 0)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(LoggerEventIds.RoleValidationFailed, "Role {roleId} validation failed: {errors}.", await GetRoleIdAsync(role).ConfigureAwait(false), string.Join(";", errors.Select(e => e.Code)));
            }
            return IdentityResult.Failed(errors);
        }
        return IdentityResult.Success;
    }

    /// <summary>
    /// Called to update the role after validating and updating the normalized role name.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>Whether the operation was successful.</returns>
    protected virtual async Task<IdentityResult> UpdateRoleAsync(TRole role)
    {
        var result = await ValidateRoleAsync(role).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result;
        }
        await UpdateNormalizedRoleNameAsync(role).ConfigureAwait(false);
        return await Store.UpdateAsync(role, CancellationToken).ConfigureAwait(false);
    }

    // IRoleClaimStore methods
    private IRoleClaimStore<TRole> GetClaimStore()
    {
        var cast = Store as IRoleClaimStore<TRole>;
        if (cast == null)
        {
            throw new NotSupportedException(Resources.StoreNotIRoleClaimStore);
        }
        return cast;
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }
}
