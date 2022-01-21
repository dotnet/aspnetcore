// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Signifies that the <see cref="RegistryPolicyResolver"/> should bind this property from the registry.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class ApplyPolicyAttribute : Attribute { }
