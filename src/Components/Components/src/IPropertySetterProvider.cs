// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

public interface IPropertySetterProvider
{
    IUnmatchedValuesPropertySetter? UnmatchedValuesPropertySetter { get; }
    bool TryGetSetter(string parameterName, [NotNullWhen(returnValue: true)] out IPropertySetter? writer);
}
