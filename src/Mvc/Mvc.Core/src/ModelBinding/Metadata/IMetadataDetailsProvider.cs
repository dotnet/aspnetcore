// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Marker interface for a provider of metadata details about model objects. Implementations should
/// implement one or more of <see cref="IBindingMetadataProvider"/>, <see cref="IDisplayMetadataProvider"/>,
/// and <see cref="IValidationMetadataProvider"/>.
/// </summary>
public interface IMetadataDetailsProvider
{
}
