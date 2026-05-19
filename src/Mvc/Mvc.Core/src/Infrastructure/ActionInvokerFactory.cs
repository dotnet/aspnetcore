// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ActionInvokerFactory : IActionInvokerFactory
{
    private readonly IActionInvokerProvider[] _actionInvokerProviders;

    public ActionInvokerFactory(IEnumerable<IActionInvokerProvider> actionInvokerProviders)
    {
        _actionInvokerProviders = actionInvokerProviders.OrderBy(item => item.Order).ToArray();
    }

    public IActionInvoker? CreateInvoker(ActionContext actionContext)
    {
        var context = new ActionInvokerProviderContext(actionContext);

        foreach (var provider in _actionInvokerProviders)
        {
            provider.OnProvidersExecuting(context);
        }

        for (var i = _actionInvokerProviders.Length - 1; i >= 0; i--)
        {
            _actionInvokerProviders[i].OnProvidersExecuted(context);
        }

        return context.Result;
    }
}
