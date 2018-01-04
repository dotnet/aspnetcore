// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Identity.Test;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class ApiConsistencyTest : ApiConsistencyTestBase
    {
        protected override Assembly TargetAssembly => typeof(IdentityUser).GetTypeInfo().Assembly;
    }
}
