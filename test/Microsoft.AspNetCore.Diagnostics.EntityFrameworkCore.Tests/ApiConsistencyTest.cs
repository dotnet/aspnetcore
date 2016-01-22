// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCoreTests
{
    public class ApiConsistencyTest : ApiConsistencyTestBase
    {
        protected override Assembly TargetAssembly => typeof(DatabaseErrorPageMiddleware).GetTypeInfo().Assembly;

        protected override IEnumerable<string> GetCancellationTokenExceptions()
        {
            return new string[] 
            {
                "DatabaseErrorPageMiddleware.Invoke",
                "MigrationsEndPointMiddleware.Invoke",
                "DatabaseErrorPage.ExecuteAsync",
                "BaseView.ExecuteAsync"
            };
        }

        protected override IEnumerable<string> GetAsyncSuffixExceptions()
        {
            return new string[]
            {
                "DatabaseErrorPageMiddleware.Invoke",
                "MigrationsEndPointMiddleware.Invoke"
            };
        }
    }
}
