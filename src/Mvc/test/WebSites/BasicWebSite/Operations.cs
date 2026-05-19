// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace BasicWebSite;

public static class Operations
{
    public static OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement { Name = "Edit" };
}
