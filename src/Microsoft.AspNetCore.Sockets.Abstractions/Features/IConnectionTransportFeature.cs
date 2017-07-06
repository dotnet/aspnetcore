// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Features
{
    public interface IConnectionTransportFeature
    {
        Channel<byte[]> Transport { get; set; }
    }
}
