// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal.Encoders;

namespace Microsoft.AspNetCore.SignalR.Features
{
    public interface IDataEncoderFeature
    {
        IDataEncoder DataEncoder { get; set; }
    }

    public class DataEncoderFeature : IDataEncoderFeature
    {
        public IDataEncoder DataEncoder { get; set; }
    }
}
