// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace CustomPolicyProvider;

internal class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int Age { get; private set; }

    public MinimumAgeRequirement(int age) { Age = age; }
}
