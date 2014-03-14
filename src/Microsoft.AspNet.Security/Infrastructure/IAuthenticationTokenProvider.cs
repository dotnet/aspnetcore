// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public interface IAuthenticationTokenProvider
    {
        void Create(AuthenticationTokenCreateContext context);
        Task CreateAsync(AuthenticationTokenCreateContext context);
        void Receive(AuthenticationTokenReceiveContext context);
        Task ReceiveAsync(AuthenticationTokenReceiveContext context);
    }
}
