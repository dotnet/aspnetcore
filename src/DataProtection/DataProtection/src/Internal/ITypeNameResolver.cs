// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal interface ITypeNameResolver
{
    bool TryResolveType(string typeName, [NotNullWhen(true)] out Type? type);
}
