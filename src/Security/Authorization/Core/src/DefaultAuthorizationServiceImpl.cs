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

internal sealed class DefaultAuthorizationServiceImpl(
    IAuthorizationPolicyProvider policyProvider,
    IAuthorizationHandlerProvider handlers,
    ILogger<DefaultAuthorizationService> logger,
    IAuthorizationHandlerContextFactory contextFactory,
    IAuthorizationEvaluator evaluator,
    IOptions<AuthorizationOptions> options,
    AuthorizationMetrics metrics)
    : DefaultAuthorizationService(policyProvider, handlers, logger, contextFactory, evaluator, options)
{
    public override async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        AuthorizationResult result;
        try
        {
            result = await base.AuthorizeAsync(user, resource, requirements).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.AuthorizedRequestCompleted(user, policyName: null, result: null, ex);
            throw;
        }

        metrics.AuthorizedRequestCompleted(user, policyName: null, result, exception: null);
        return result;
    }

    public override async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        AuthorizationResult result;
        try
        {
            var policy = await GetPolicyAsync(policyName).ConfigureAwait(false);

            // Note that we deliberately call the base method of the other overload here.
            // This is because the base implementation for this overload dispatches to the other overload,
            // which would cause metrics to be recorded twice.
            result = await base.AuthorizeAsync(user, resource, policy.Requirements).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.AuthorizedRequestCompleted(user, policyName, result: null, ex);
            throw;
        }

        metrics.AuthorizedRequestCompleted(user, policyName, result, exception: null);
        return result;
    }
}
