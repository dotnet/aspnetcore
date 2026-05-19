// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Identity.Test;

public class ApiConsistencyTest : ApiConsistencyTestBase
{
    protected override Assembly TargetAssembly => typeof(IdentityOptions).Assembly;
}
