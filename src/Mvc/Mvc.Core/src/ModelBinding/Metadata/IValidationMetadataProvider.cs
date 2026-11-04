// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Provides <see cref="ValidationMetadata"/> for a <see cref="DefaultModelMetadata"/>.
/// </summary>
public interface IValidationMetadataProvider : IMetadataDetailsProvider
{
    /// <summary>
    /// Gets the values for properties of <see cref="ValidationMetadata"/>.
    /// </summary>
    /// <param name="context">The <see cref="ValidationMetadataProviderContext"/>.</param>
    void CreateValidationMetadata(ValidationMetadataProviderContext context);
}
