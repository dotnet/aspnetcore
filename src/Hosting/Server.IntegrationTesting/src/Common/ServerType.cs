// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public enum ServerType
{
    None,
    IISExpress,
    IIS,
    HttpSys,
    Kestrel,
    Nginx
}
