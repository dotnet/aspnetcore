// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Signifies that the <see cref="RegistryPolicyResolver"/> should bind this property from the registry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class ApplyPolicyAttribute : Attribute { }
}
