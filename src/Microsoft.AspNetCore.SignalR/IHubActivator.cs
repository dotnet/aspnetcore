// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubActivator<THub> where THub : Hub
    {
        THub Create();
        void Release(THub hub);
    }
}
