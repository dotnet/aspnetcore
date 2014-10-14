// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents an <see cref="IMarkerAwareBinder"/> which can select itself based on the
    /// <typeparamref name="TBinderMarker"/>.
    /// </summary>
    /// <typeparam name="TBinderMarker">Represents a type implementing <see cref="IBinderMarker"/></typeparam>
    public abstract class MarkerAwareBinder<TBinderMarker> : IMarkerAwareBinder
        where TBinderMarker : IBinderMarker
    {
        /// <summary>
        /// Async function which does the actual binding to bind to a particular model.
        /// </summary>
        /// <param name="bindingContext">The binding context which has the object to be bound.</param>
        /// <param name="marker">The <see cref="IBinderMarker"/> associated with the current binder.</param>
        /// <returns>A Task with a bool implying the success or failure of the operation.</returns>
        protected abstract Task<bool> BindAsync(ModelBindingContext bindingContext, TBinderMarker marker);

        public Task<bool> BindModelAsync(ModelBindingContext context)
        {
            if (context.ModelMetadata.Marker is TBinderMarker)
            {
                var marker = (TBinderMarker)context.ModelMetadata.Marker;
                return BindAsync(context, marker);
            }

            return Task.FromResult(false);
        }       
    }
}
