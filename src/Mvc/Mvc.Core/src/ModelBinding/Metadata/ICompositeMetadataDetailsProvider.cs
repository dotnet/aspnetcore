// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A composite <see cref="IMetadataDetailsProvider"/>.
    /// </summary>
    public interface ICompositeMetadataDetailsProvider : 
        IBindingMetadataProvider, 
        IDisplayMetadataProvider, 
        IValidationMetadataProvider
    {
    }
}