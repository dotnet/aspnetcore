// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IMetadataAwareValueProvider"/> value provider which can filter
    /// based on <see cref="IValueProviderMetadata"/>.
    /// </summary>
    /// <typeparam name="TBinderMetadata">
    /// Represents a type implementing <see cref="IValueProviderMetadata"/>
    /// </typeparam>
    public abstract class MetadataAwareValueProvider<TBinderMetadata> : IMetadataAwareValueProvider
        where TBinderMetadata : IValueProviderMetadata
    {
        public abstract Task<bool> ContainsPrefixAsync(string prefix);

        public abstract Task<ValueProviderResult> GetValueAsync(string key);

        public virtual IValueProvider Filter(IValueProviderMetadata valueBinderMetadata)
        {
            if (valueBinderMetadata is TBinderMetadata)
            {
                return this;
            }

            return null;
        }
    }
}
