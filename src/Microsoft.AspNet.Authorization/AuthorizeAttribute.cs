// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute() { }

        public AuthorizeAttribute(string policy)
        {
            Policy = policy;
        }

        public string Policy { get; set; }

        // REVIEW: can we get rid of the , deliminated in Roles/AuthTypes
        public string Roles { get; set; }

        public string ActiveAuthenticationSchemes { get; set; }
    }
}
