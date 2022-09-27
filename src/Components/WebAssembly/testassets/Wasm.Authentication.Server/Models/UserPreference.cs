// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Authentication.Server.Models;

public class UserPreference
{
    public string Id { get; set; }

    public string ApplicationUserId { get; set; }

    public string Color { get; set; }
}
