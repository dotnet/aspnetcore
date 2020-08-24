// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public interface IHttpSysRequestDelegationFeature
    {
        /// <summary>
        /// Indicates if the server can delegate this request to another HttpSys request queue.
        /// </summary>
        bool CanDelegate { get; }

        /// <summary>
        /// Attempt to delegate the request to another Http.Sys request queue. The request body
        /// must not be read nor the response started before this is invoked. Check <see cref="CanDelegate"/>
        /// before invoking.
        /// </summary>
        void DelegateRequest(DelegationRule destination);
    }
}
