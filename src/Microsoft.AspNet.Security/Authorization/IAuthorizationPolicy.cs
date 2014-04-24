// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;
namespace Microsoft.AspNet.Security.Authorization
{
    public interface IAuthorizationPolicy
    {
        int Order { get; set; }
        Task ApplyingAsync(AuthorizationPolicyContext context);
        Task ApplyAsync(AuthorizationPolicyContext context);
        Task AppliedAsync(AuthorizationPolicyContext context);
    }
}
