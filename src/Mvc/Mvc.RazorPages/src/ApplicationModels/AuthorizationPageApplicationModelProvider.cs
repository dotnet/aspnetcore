// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class AuthorizationPageApplicationModelProvider : IPageApplicationModelProvider
{
    private readonly IAuthorizationPolicyProvider _policyProvider;
    private readonly MvcOptions _mvcOptions;

    public AuthorizationPageApplicationModelProvider(
        IAuthorizationPolicyProvider policyProvider,
        IOptions<MvcOptions> mvcOptions)
    {
        _policyProvider = policyProvider;
        _mvcOptions = mvcOptions.Value;
    }

    // The order is set to execute after the DefaultPageApplicationModelProvider.
    public int Order => -1000 + 10;

    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_mvcOptions.EnableEndpointRouting)
        {
            // When using endpoint routing, the AuthorizationMiddleware does the work that Auth filters would otherwise perform.
            // Consequently we do not need to convert authorization attributes to filters.
            return;
        }

        var pageModel = context.PageApplicationModel;
        var authorizeData = pageModel.HandlerTypeAttributes.OfType<IAuthorizeData>().ToArray();
        if (authorizeData.Length > 0)
        {
            pageModel.Filters.Add(AuthorizationApplicationModelProvider.GetFilter(_policyProvider, authorizeData));
        }
        foreach (var _ in pageModel.HandlerTypeAttributes.OfType<IAllowAnonymous>())
        {
            pageModel.Filters.Add(new AllowAnonymousFilter());
        }
    }

    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }
}
