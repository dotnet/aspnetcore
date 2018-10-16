// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    // Single implementation of this interface is conditionally added to DI on Windows
    // We have to use interface because some DI implementations would try to activate class
    // even if it was not registered causing problems crossplat
    internal interface IRegistryPolicyResolver
    {
        RegistryPolicy ResolvePolicy();
    }
}