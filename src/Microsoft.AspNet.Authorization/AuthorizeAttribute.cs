// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Authorization
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied to requires the specified authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizeData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class. 
        /// </summary>
        public AuthorizeAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with the specified policy. 
        /// </summary>
        /// <param name="policy">The name of the policy to require for authorization.</param>
        public AuthorizeAttribute(string policy)
        {
            Policy = policy;
        }

        /// <inheritdoc />
        public string Policy { get; set; }

        /// <inheritdoc />
        // REVIEW: can we get rid of the , deliminated in Roles/AuthTypes
        public string Roles { get; set; }

        /// <inheritdoc />
        public string ActiveAuthenticationSchemes { get; set; }
    }
}
