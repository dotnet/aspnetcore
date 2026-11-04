// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace ApiExplorerWebSite;

public class ActionDescriptorChangeProvider : IActionDescriptorChangeProvider
{
    public ActionDescriptorChangeProvider(WellKnownChangeToken changeToken)
    {
        ChangeToken = changeToken;
    }

    public WellKnownChangeToken ChangeToken { get; }

    public IChangeToken GetChangeToken()
    {
        if (ChangeToken.TokenSource.IsCancellationRequested)
        {
            var changeTokenSource = new CancellationTokenSource();
            return new CancellationChangeToken(changeTokenSource.Token);
        }

        return new CancellationChangeToken(ChangeToken.TokenSource.Token);
    }
}
