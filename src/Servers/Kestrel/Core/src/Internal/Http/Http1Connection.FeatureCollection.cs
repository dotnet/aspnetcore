// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class Http1Connection : IHttpMinRequestBodyDataRateFeature,
                                           IHttpMinResponseDataRateFeature
    {
        MinDataRate IHttpMinRequestBodyDataRateFeature.MinDataRate
        {
            get => MinRequestBodyDataRate;
            set => MinRequestBodyDataRate = value;
        }

        MinDataRate IHttpMinResponseDataRateFeature.MinDataRate
        {
            get => MinResponseDataRate;
            set => MinResponseDataRate = value;
        }
    }
}
