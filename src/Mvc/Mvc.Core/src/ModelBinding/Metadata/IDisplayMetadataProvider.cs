// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Provides <see cref="DisplayMetadata"/> for a <see cref="DefaultModelMetadata"/>.
/// </summary>
public interface IDisplayMetadataProvider : IMetadataDetailsProvider
{
    /// <summary>
    /// Sets the values for properties of <see cref="DisplayMetadataProviderContext.DisplayMetadata"/>.
    /// </summary>
    /// <param name="context">The <see cref="DisplayMetadataProviderContext"/>.</param>
    void CreateDisplayMetadata(DisplayMetadataProviderContext context);
}
