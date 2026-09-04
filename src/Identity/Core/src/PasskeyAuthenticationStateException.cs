// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

internal sealed class PasskeyAuthenticationStateException(string message) : InvalidOperationException(message);
