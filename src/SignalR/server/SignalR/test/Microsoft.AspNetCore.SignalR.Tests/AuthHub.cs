// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.SignalR.Tests;

[Authorize]
class AuthHub : Hub
{
}
