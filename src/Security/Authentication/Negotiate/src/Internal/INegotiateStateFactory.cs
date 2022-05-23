// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

// For testing
internal interface INegotiateStateFactory
{
    [RequiresUnreferencedCode("Negotiate authentication uses types that cannot be statically analyzed.")]
    INegotiateState CreateInstance();
}
