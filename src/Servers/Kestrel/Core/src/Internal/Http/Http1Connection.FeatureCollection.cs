// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class Http1Connection : IHttpMinRequestBodyDataRateFeature,
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
