// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
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

        public IDisposable StartServer<T>(out InProcessTestServer<T> testServer, Func<WriteContext, bool> expectedErrorsFilter = null) where T : class
        {
            var disposable = base.StartVerifiableLog(ResolveExpectedErrorsFilter(expectedErrorsFilter));
            testServer = new InProcessTestServer<T>(LoggerFactory);
            return new MultiDisposable(testServer, disposable);
        }

        private class MultiDisposable : IDisposable
        {
            List<IDisposable> _disposables;
            public MultiDisposable(params IDisposable[] disposables)
            {
                _disposables = new List<IDisposable>(disposables);
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}