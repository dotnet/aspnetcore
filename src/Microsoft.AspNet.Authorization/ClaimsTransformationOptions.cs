// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authorization
{
    public class ClaimsTransformationOptions
    {
        public Func<ClaimsPrincipal, Task<ClaimsPrincipal>> TransformAsync { get; set; }
    }
}
