// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents an <see cref="IMetadataAwareBinder"/> which can select itself based on the
    /// <typeparamref name="TBinderMetadata"/>.
    /// </summary>
    /// <typeparam name="TBinderMetadata">Represents a type implementing <see cref="IBinderMetadata"/></typeparam>
    public abstract class MetadataAwareBinder<TBinderMetadata> : IMetadataAwareBinder
        where TBinderMetadata : IBinderMetadata
    {
        /// <summary>
        /// Async function which does the actual binding to bind to a particular model.
        /// </summary>
        /// <param name="bindingContext">The binding context which has the object to be bound.</param>
        /// <param name="metadata">The <see cref="IBinderMetadata"/> associated with the current binder.</param>
        /// <returns>A Task with a bool implying the success or failure of the operation.</returns>
        protected abstract Task<bool> BindAsync(ModelBindingContext bindingContext, TBinderMetadata metadata);

        public Task<bool> BindModelAsync(ModelBindingContext context)
        {
            if (context.ModelMetadata.BinderMetadata is TBinderMetadata)
            {
                var metadata = (TBinderMetadata)context.ModelMetadata.BinderMetadata;
                return BindAsync(context, metadata);
            }

            return Task.FromResult(false);
        }       
    }
}
