// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MvcSandbox.AuthorizationMiddleware
{
    public class AuthorizeMetadataAttribute : Attribute
    {
        public AuthorizeMetadataAttribute(string[] roles)
        {
            Roles = roles;
        }

        public string[] Roles { get; }
    }
}
