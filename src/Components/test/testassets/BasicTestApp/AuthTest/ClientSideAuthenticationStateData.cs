// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp.AuthTest;

// DTO shared between server and client
public class ClientSideAuthenticationStateData
{
    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; }

    public List<ExposedClaim> ExposedClaims { get; set; }
}

public class ExposedClaim
{
    public string Type { get; set; }
    public string Value { get; set; }
}
