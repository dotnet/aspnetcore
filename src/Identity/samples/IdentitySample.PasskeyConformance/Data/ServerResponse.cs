// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IdentitySample.PasskeyConformance.Data;

internal abstract class ServerResponse(string status, string errorMessage = "")
{
    public string Status { get; } = status;
    public string ErrorMessage { get; } = errorMessage;
}
