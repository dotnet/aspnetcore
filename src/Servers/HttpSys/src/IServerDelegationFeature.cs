// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public interface IServerDelegationFeature
    {
        /// <summary>
        /// Create a delegation rule on request queue owned by the server.
        /// </summary>
        /// <returns>
        /// Creates a <see cref="DelegationRule"/> that can used to delegate individual requests.
        /// </returns>
        DelegationRule CreateDelegationRule(string queueName, string urlPrefix);
    }
}
