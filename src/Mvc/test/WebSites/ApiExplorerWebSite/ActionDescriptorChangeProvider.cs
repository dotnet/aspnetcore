// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace ApiExplorerWebSite
{
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
}
