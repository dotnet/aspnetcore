// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace Microsoft.AspNetCore.Components.Authorization;

public class TestIdentity : IIdentity
{
    public string AuthenticationType => "Test";

    public bool IsAuthenticated => true;

    public string Name { get; set; }
}
