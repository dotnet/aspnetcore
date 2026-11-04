// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

internal static class HandleRequestResults
{
    internal static HandleRequestResult InvalidStateCookie = HandleRequestResult.Fail("Invalid state cookie.");
}
