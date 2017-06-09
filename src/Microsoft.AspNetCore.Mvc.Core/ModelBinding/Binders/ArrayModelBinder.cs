// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding array values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the array.</typeparam>
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        /// <summary>
        /// Creates a new <see cref="ArrayModelBinder{TElement}"/>.
        /// </summary>
        /// <param name="elementBinder">
        /// The <see cref="IModelBinder"/> for binding <typeparamref name="TElement"/>.
        /// </param>
        public ArrayModelBinder(IModelBinder elementBinder)
            : base(elementBinder)
        {
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

            return Array.Empty<TElement>();
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
            // consistent with our handling of a read-only array property.
        }
    }
}
