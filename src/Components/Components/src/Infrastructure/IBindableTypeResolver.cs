// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides the compile-time description of a form model type, used to evaluate a binding expression
/// without compiling a delegate.
/// </summary>
internal interface IBindableTypeResolver
{
    /// <summary>
    /// Looks up the descriptor for a type appearing in a binding expression chain.
    /// </summary>
    bool TryGetBindableTypeDescriptor(Type type, [NotNullWhen(true)] out BindableTypeDescriptor? descriptor);
}
