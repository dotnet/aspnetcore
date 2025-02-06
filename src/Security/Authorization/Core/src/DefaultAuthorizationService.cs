// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// The default implementation of an <see cref="IAuthorizationService"/>.
/// </summary>
public class DefaultAuthorizationService : IAuthorizationService
{
    private readonly AuthorizationOptions _options;
    private readonly IAuthorizationHandlerContextFactory _contextFactory;
    private readonly IAuthorizationHandlerProvider _handlers;
    private readonly IAuthorizationEvaluator _evaluator;
    private readonly IAuthorizationPolicyProvider _policyProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultAuthorizationService"/>.
    /// </summary>
    /// <param name="policyProvider">The <see cref="IAuthorizationPolicyProvider"/> used to provide policies.</param>
    /// <param name="handlers">The handlers used to fulfill <see cref="IAuthorizationRequirement"/>s.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    /// <param name="contextFactory">The <see cref="IAuthorizationHandlerContextFactory"/> used to create the context to handle the authorization.</param>
    /// <param name="evaluator">The <see cref="IAuthorizationEvaluator"/> used to determine if authorization was successful.</param>
    /// <param name="options">The <see cref="AuthorizationOptions"/> used.</param>
    public DefaultAuthorizationService(IAuthorizationPolicyProvider policyProvider, IAuthorizationHandlerProvider handlers, ILogger<DefaultAuthorizationService> logger, IAuthorizationHandlerContextFactory contextFactory, IAuthorizationEvaluator evaluator, IOptions<AuthorizationOptions> options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(options);
        ArgumentNullThrowHelper.ThrowIfNull(policyProvider);
        ArgumentNullThrowHelper.ThrowIfNull(handlers);
        ArgumentNullThrowHelper.ThrowIfNull(logger);
        ArgumentNullThrowHelper.ThrowIfNull(contextFactory);
        ArgumentNullThrowHelper.ThrowIfNull(evaluator);

        _options = options.Value;
        _handlers = handlers;
        _policyProvider = policyProvider;
        _logger = logger;
        _evaluator = evaluator;
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Checks if a user meets a specific set of requirements for the specified resource.
    /// </summary>
    /// <param name="user">The user to evaluate the requirements against.</param>
    /// <param name="resource">The resource to evaluate the requirements against.</param>
    /// <param name="requirements">The requirements to evaluate.</param>
    /// <returns>
    /// A flag indicating whether authorization has succeeded.
    /// This value is <c>true</c> when the user fulfills the policy, otherwise <c>false</c>.
    /// </returns>
    public virtual async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        ArgumentNullThrowHelper.ThrowIfNull(requirements);

        var authContext = _contextFactory.CreateContext(requirements, user, resource);
        var handlers = await _handlers.GetHandlersAsync(authContext).ConfigureAwait(false);
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(authContext).ConfigureAwait(false);
            if (!_options.InvokeHandlersAfterFailure && authContext.HasFailed)
            {
                break;
            }
        }

        var result = _evaluator.Evaluate(authContext);
        if (result.Succeeded)
        {
            _logger.UserAuthorizationSucceeded();
        }
        else
        {
            _logger.UserAuthorizationFailed(result.Failure);
        }
        return result;
    }

    /// <summary>
    /// Checks if a user meets a specific authorization policy.
    /// </summary>
    /// <param name="user">The user to check the policy against.</param>
    /// <param name="resource">The resource the policy should be checked with.</param>
    /// <param name="policyName">The name of the policy to check against a specific context.</param>
    /// <returns>
    /// A flag indicating whether authorization has succeeded.
    /// This value is <c>true</c> when the user fulfills the policy otherwise <c>false</c>.
    /// </returns>
    public virtual async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        var policy = await GetPolicyAsync(policyName).ConfigureAwait(false);
        return await this.AuthorizeAsync(user, resource, policy).ConfigureAwait(false);
    }

    // For use in DefaultAuthorizationServiceImpl.
    private protected async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policyName);
        return await _policyProvider.GetPolicyAsync(policyName).ConfigureAwait(false) ?? throw new InvalidOperationException($"No policy found: {policyName}.");
    }
}
