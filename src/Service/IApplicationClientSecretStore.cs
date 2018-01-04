// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IApplicationClientSecretStore<TApplication> 
        : IApplicationStore<TApplication> where TApplication : class
    {
        Task SetClientSecretHashAsync(TApplication application, string clientSecretHash, CancellationToken cancellationToken);
        Task<string> GetClientSecretHashAsync(TApplication application, CancellationToken cancellationToken);
        Task<bool> HasClientSecretAsync(TApplication application, CancellationToken cancellationToken);
    }
}
