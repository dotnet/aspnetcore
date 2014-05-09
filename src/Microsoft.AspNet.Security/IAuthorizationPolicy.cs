// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    public interface IAuthorizationPolicy
    {
        int Order { get; set; }
        Task ApplyingAsync(AuthorizationPolicyContext context);
        Task ApplyAsync(AuthorizationPolicyContext context);
        Task AppliedAsync(AuthorizationPolicyContext context);
    }
}
