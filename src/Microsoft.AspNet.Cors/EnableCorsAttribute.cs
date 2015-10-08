// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cors.Infrastructure;

namespace Microsoft.AspNet.Cors
{
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EnableCorsAttribute : Attribute, IEnableCorsAttribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="EnableCorsAttribute"/>.
        /// </summary>
        /// <param name="policyName">The name of the policy to be applied.</param>
        public EnableCorsAttribute(string policyName)
        {
            PolicyName = policyName;
        }

        /// <inheritdoc />
        public string PolicyName { get; set; }
    }
}