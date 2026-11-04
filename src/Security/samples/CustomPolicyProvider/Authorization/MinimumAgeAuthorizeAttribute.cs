// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authorization;

namespace CustomPolicyProvider;

// This attribute derives from the [Authorize] attribute, adding
// the ability for a user to specify an 'age' paratmer. Since authorization
// policies are looked up from the policy provider only by string, this
// authorization attribute creates is policy name based on a constant prefix
// and the user-supplied age parameter. A custom authorization policy provider
// (`MinimumAgePolicyProvider`) can then produce an authorization policy with
// the necessary requirements based on this policy name.
internal class MinimumAgeAuthorizeAttribute : AuthorizeAttribute
{
    const string POLICY_PREFIX = "MinimumAge";

    public MinimumAgeAuthorizeAttribute(int age) => Age = age;

    // Get or set the Age property by manipulating the underlying Policy property
    public int Age
    {
        get
        {
            if (int.TryParse(Policy.AsSpan(POLICY_PREFIX.Length), out var age))
            {
                return age;
            }
            return default(int);
        }
        set
        {
            Policy = $"{POLICY_PREFIX}{value}";
        }
    }
}
