// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IAuthorizationResponseParameterProvider
    {
        int Order { get; }

        Task AddParameters(TokenGeneratingContext context, AuthorizationResponse response);
    }
}
