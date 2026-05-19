// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class AuthorizationApplicationModelProvider : IApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions;
    private readonly IAuthorizationPolicyProvider _policyProvider;

    public AuthorizationApplicationModelProvider(
        IAuthorizationPolicyProvider policyProvider,
        IOptions<MvcOptions> mvcOptions)
    {
        _policyProvider = policyProvider;
        _mvcOptions = mvcOptions.Value;
    }

    public int Order => -1000 + 10;

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // Intentionally empty.
    }

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_mvcOptions.EnableEndpointRouting)
        {
            // When using endpoint routing, the AuthorizationMiddleware does the work that Auth filters would otherwise perform.
            // Consequently we do not need to convert authorization attributes to filters.
            return;
        }

        foreach (var controllerModel in context.Result.Controllers)
        {
            var controllerModelAuthData = controllerModel.Attributes.OfType<IAuthorizeData>().ToArray();
            if (controllerModelAuthData.Length > 0)
            {
                controllerModel.Filters.Add(GetFilter(_policyProvider, controllerModelAuthData));
            }
            foreach (var attribute in controllerModel.Attributes.OfType<IAllowAnonymous>())
            {
                controllerModel.Filters.Add(new AllowAnonymousFilter());
            }

            foreach (var actionModel in controllerModel.Actions)
            {
                var actionModelAuthData = actionModel.Attributes.OfType<IAuthorizeData>().ToArray();
                if (actionModelAuthData.Length > 0)
                {
                    actionModel.Filters.Add(GetFilter(_policyProvider, actionModelAuthData));
                }

                foreach (var _ in actionModel.Attributes.OfType<IAllowAnonymous>())
                {
                    actionModel.Filters.Add(new AllowAnonymousFilter());
                }
            }
        }
    }

    public static AuthorizeFilter GetFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authData)
    {
        // The default policy provider will make the same policy for given input, so make it only once.
        // This will always execute synchronously.
        if (policyProvider.GetType() == typeof(DefaultAuthorizationPolicyProvider))
        {
            var policy = AuthorizationPolicy.CombineAsync(policyProvider, authData).GetAwaiter().GetResult()!;
            return new AuthorizeFilter(policy);
        }
        else
        {
            return new AuthorizeFilter(policyProvider, authData);
        }
    }
}
