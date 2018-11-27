// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifiableLoggedTest : LoggedTest
    {
        public virtual IDisposable StartVerifiableLog(Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            return CreateScope(expectedErrorsFilter);
        }

        private VerifyNoErrorsScope CreateScope(Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            return new VerifyNoErrorsScope(LoggerFactory, wrappedDisposable: null, expectedErrorsFilter);
        }
    }
}
