// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class UpgradeFeature : IHttpUpgradeFeature
    {
        public bool IsUpgradableRequest => true;

        // TestHost provides an IHttpWebSocketFeature so it wont call UpgradeAsync()
        public Task<Stream> UpgradeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
