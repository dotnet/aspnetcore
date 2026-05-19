// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Provides <see cref="BindingMetadata"/> for a <see cref="DefaultModelMetadata"/>.
/// </summary>
public interface IBindingMetadataProvider : IMetadataDetailsProvider
{
    /// <summary>
    /// Sets the values for properties of <see cref="BindingMetadataProviderContext.BindingMetadata"/>.
    /// </summary>
    /// <param name="context">The <see cref="BindingMetadataProviderContext"/>.</param>
    void CreateBindingMetadata(BindingMetadataProviderContext context);
}
