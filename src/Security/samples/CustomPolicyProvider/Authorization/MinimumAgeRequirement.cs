// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace CustomPolicyProvider
{
    internal class MinimumAgeRequirement : IAuthorizationRequirement
    {
        public int Age { get; private set; }

        public MinimumAgeRequirement(int age) { Age = age; }
    }
}