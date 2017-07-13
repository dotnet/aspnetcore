// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Sockets.Features;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class TransferModeFeature : ITransferModeFeature
    {
        public TransferMode TransferMode { get; set; }
    }
}
