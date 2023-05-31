// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

internal interface ICascadingValueSupplierFactory
{
    bool TryGetValueSupplier(object propertyAttribute, Type valueType, string? valueName, [NotNullWhen(true)] out ICascadingValueSupplier? result);
}
