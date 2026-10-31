// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Contains authorization information used by <see cref="IAuthorizationHandler"/>.
/// </summary>
public class AuthorizationHandlerContext
{
    private readonly HashSet<IAuthorizationRequirement> _pendingRequirements;
    private List<AuthorizationFailureReason>? _failedReasons;
    private bool _failCalled;
    private bool _succeedCalled;

    /// <summary>
    /// Creates a new instance of <see cref="AuthorizationHandlerContext"/>.
    /// </summary>
    /// <param name="requirements">A collection of all the <see cref="IAuthorizationRequirement"/> for the current authorization action.</param>
    /// <param name="user">A <see cref="ClaimsPrincipal"/> representing the current user.</param>
    /// <param name="resource">An optional resource to evaluate the <paramref name="requirements"/> against.</param>
    public AuthorizationHandlerContext(
        IEnumerable<IAuthorizationRequirement> requirements,
        ClaimsPrincipal user,
        object? resource)
    {
        ArgumentNullThrowHelper.ThrowIfNull(requirements);

        Requirements = requirements;
        User = user;
        Resource = resource;
        _pendingRequirements = new HashSet<IAuthorizationRequirement>(requirements);
    }

    /// <summary>
    /// The collection of all the <see cref="IAuthorizationRequirement"/> for the current authorization action.
    /// </summary>
    public virtual IEnumerable<IAuthorizationRequirement> Requirements { get; }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> representing the current user.
    /// </summary>
    public virtual ClaimsPrincipal User { get; }

    /// <summary>
    /// The optional resource to evaluate the <see cref="AuthorizationHandlerContext.Requirements"/> against.
    /// </summary>
    public virtual object? Resource { get; }

    /// <summary>
    /// Gets the requirements that have not yet been marked as succeeded.
    /// </summary>
    public virtual IEnumerable<IAuthorizationRequirement> PendingRequirements { get { return _pendingRequirements; } }

    /// <summary>
    /// Gets the reasons why authorization has failed.
    /// </summary>
    public virtual IEnumerable<AuthorizationFailureReason> FailureReasons
        => (IEnumerable<AuthorizationFailureReason>?)_failedReasons ?? Array.Empty<AuthorizationFailureReason>();

    /// <summary>
    /// Flag indicating whether the current authorization processing has failed due to Fail being called.
    /// </summary>
    public virtual bool HasFailed { get { return _failCalled; } }

    /// <summary>
    /// Flag indicating whether the current authorization processing has succeeded.
    /// </summary>
    public virtual bool HasSucceeded
    {
        get
        {
            return !_failCalled && _succeedCalled && !PendingRequirements.Any();
        }
    }

    /// <summary>
    /// Called to indicate <see cref="AuthorizationHandlerContext.HasSucceeded"/> will
    /// never return true, even if all requirements are met.
    /// </summary>
    public virtual void Fail()
    {
        _failCalled = true;
    }

    /// <summary>
    /// Called to indicate <see cref="AuthorizationHandlerContext.HasSucceeded"/> will
    /// never return true, even if all requirements are met.
    /// </summary>
    /// <param name="reason">Optional <see cref="AuthorizationFailureReason"/> for why authorization failed.</param>
    public virtual void Fail(AuthorizationFailureReason reason)
    {
        Fail();
        if (reason != null)
        {
            if (_failedReasons == null)
            {
                _failedReasons = new List<AuthorizationFailureReason>();
            }

            _failedReasons.Add(reason);
        }
    }

    /// <summary>
    /// Called to mark the specified <paramref name="requirement"/> as being
    /// successfully evaluated.
    /// </summary>
    /// <param name="requirement">The requirement whose evaluation has succeeded.</param>
    public virtual void Succeed(IAuthorizationRequirement requirement)
    {
        _succeedCalled = true;
        _pendingRequirements.Remove(requirement);
    }
}
