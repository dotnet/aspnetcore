// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Provides a <see cref="Type"/> which implements <see cref="IModelBinder"/>.
/// </summary>
public interface IBinderTypeProviderMetadata : IBindingSourceMetadata
{
    /// <summary>
    /// A <see cref="Type"/> which implements either <see cref="IModelBinder"/>.
    /// </summary>
    Type? BinderType { get; }
}
