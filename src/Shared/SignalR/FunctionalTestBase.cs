// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class FunctionalTestBase : VerifiableLoggedTest
{
    private readonly Func<WriteContext, bool> _globalExpectedErrorsFilter;

    public FunctionalTestBase()
    {
        // Suppress errors globally here
        _globalExpectedErrorsFilter = (writeContext) => false;
    }

    private Func<WriteContext, bool> ResolveExpectedErrorsFilter(Func<WriteContext, bool> expectedErrorsFilter)
    {
        if (expectedErrorsFilter == null)
        {
            return _globalExpectedErrorsFilter;
        }

        return (writeContext) =>
        {
            if (expectedErrorsFilter(writeContext))
            {
                return true;
            }

            return _globalExpectedErrorsFilter(writeContext);
        };
    }

    public Task<InProcessTestServer<T>> StartServer<T>(Func<WriteContext, bool> expectedErrorsFilter = null, Action<KestrelServerOptions> configureKestrelServerOptions = null) where T : class
    {
        var disposable = base.StartVerifiableLog(ResolveExpectedErrorsFilter(expectedErrorsFilter));
        return InProcessTestServer<T>.StartServer(LoggerFactory, configureKestrelServerOptions, disposable);
    }
}
