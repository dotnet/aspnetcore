// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
#pragma warning disable CA1852 // Seal internal types
internal class AssemblyWrapper
#pragma warning restore CA1852 // Seal internal types
{
    public AssemblyWrapper(Assembly assembly)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);

        Assembly = assembly;
    }

    public Assembly Assembly { get; }

    public virtual string FullName => Assembly.FullName!;

    public virtual Stream? GetManifestResourceStream(string name) => Assembly.GetManifestResourceStream(name);
}
