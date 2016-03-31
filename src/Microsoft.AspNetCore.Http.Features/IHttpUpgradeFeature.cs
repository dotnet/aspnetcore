// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public interface IHttpUpgradeFeature
    {
        /// <summary>
        /// Indicates if the server can upgrade this request to an opaque, bidirectional stream.
        /// </summary>
        bool IsUpgradableRequest { get; }

        /// <summary>
        /// Attempt to upgrade the request to an opaque, bidirectional stream. The response status code
        /// and headers need to be set before this is invoked. Check <see cref="IsUpgradableRequest"/>
        /// before invoking.
        /// </summary>
        /// <returns></returns>
        Task<Stream> UpgradeAsync();
    }
}