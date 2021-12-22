﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal interface IHeartbeatHandler
{
    void OnHeartbeat(DateTimeOffset now);
}
