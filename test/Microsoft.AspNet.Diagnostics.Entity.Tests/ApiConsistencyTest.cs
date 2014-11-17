// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.Data.Entity;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Diagnostics.EntityTests
{
    public class ApiConsistencyTest : ApiConsistencyTestBase
    {
        protected override Assembly TargetAssembly
        {
            get { return typeof(DatabaseErrorPageMiddleware).Assembly; }
        }

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
