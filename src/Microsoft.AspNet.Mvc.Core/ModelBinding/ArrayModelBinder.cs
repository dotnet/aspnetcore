// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding array values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the array.</typeparam>
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        /// <inheritdoc />
        public override Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (bindingContext.ModelMetadata.IsReadOnly)
            {
                return ModelBindingResult.NoResultAsync;
            }

            return base.BindModelAsync(bindingContext);
        }

        /// <inheritdoc />
        public override bool CanCreateInstance(Type targetType)
        {
            return targetType == typeof(TElement[]);
        }

        /// <inheritdoc />
        protected override object CreateEmptyCollection(Type targetType)
        {
            Debug.Assert(targetType == typeof(TElement[]), "GenericModelBinder only creates this binder for arrays.");

            return new TElement[0];
        }

        /// <inheritdoc />
        protected override object ConvertToCollectionType(Type targetType, IEnumerable<TElement> collection)
        {
            Debug.Assert(targetType == typeof(TElement[]), "GenericModelBinder only creates this binder for arrays.");

            // If non-null, collection is a List<TElement>, never already a TElement[].
            return collection?.ToArray();
        }

        /// <inheritdoc />
        protected override void CopyToModel(object target, IEnumerable<TElement> sourceCollection)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            // Do not attempt to copy values into an array because an array's length is immutable. This choice is also
            // consistent with MutableObjectModelBinder's handling of a read-only array property.
        }
    }
}
