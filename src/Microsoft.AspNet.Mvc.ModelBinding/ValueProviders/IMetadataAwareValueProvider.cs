// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A value provider which is aware of <see cref="IValueProviderMetadata"/>.
    /// </summary>
    public interface IMetadataAwareValueProvider : IValueProvider
    {
        /// <summary>
        /// Filters the value provider based on <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="IValueProviderMetadata"/> associated with a model.</param>
        /// <returns>The filtered value provider.</returns>
        IValueProvider Filter([NotNull] IValueProviderMetadata metadata);
    }
}
