// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public interface IAuthenticationHandler
    {
        void GetDescriptions(DescribeSchemesContext context);

        Task AuthenticateAsync(AuthenticateContext context);

        Task ChallengeAsync(ChallengeContext context);

        Task SignInAsync(SignInContext context);

        Task SignOutAsync(SignOutContext context);
    }
}
