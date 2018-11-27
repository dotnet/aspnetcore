// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Enumeration for the kinds of <see cref="ModelMetadata"/>
    /// </summary>
    public enum ModelMetadataKind
    {
        /// <summary>
        /// Used for <see cref="ModelMetadata"/> for a <see cref="System.Type"/>.
        /// </summary>
        Type,

        /// <summary>
        /// Used for <see cref="ModelMetadata"/> for a property.
        /// </summary>
        Property,

        /// <summary>
        /// Used for <see cref="ModelMetadata"/> for a parameter.
        /// </summary>
        Parameter,
    }
}