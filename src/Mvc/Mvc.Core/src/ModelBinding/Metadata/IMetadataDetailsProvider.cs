// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Marker interface for a provider of metadata details about model objects. Implementations should
    /// implement one or more of <see cref="IBindingMetadataProvider"/>, <see cref="IDisplayMetadataProvider"/>, 
    /// and <see cref="IValidationMetadataProvider"/>.
    /// </summary>
    public interface IMetadataDetailsProvider
    {
    }
}
