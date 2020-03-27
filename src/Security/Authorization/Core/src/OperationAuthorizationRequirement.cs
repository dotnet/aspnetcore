// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    /// <summary>
    /// A helper class to provide a useful <see cref="IAuthorizationRequirement"/> which
    /// contains a name.
    /// </summary>
    public class OperationAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The name of this instance of <see cref="IAuthorizationRequirement"/>.
        /// </summary>
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{nameof(OperationAuthorizationRequirement)}:Name={Name}";
        }
    }
}
