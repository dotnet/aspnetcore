// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding array values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the array.</typeparam>
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        /// <inheritdoc />
        public override Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.IsReadOnly)
            {
                return Task.FromResult<ModelBindingResult>(null);
            }

            return base.BindModelAsync(bindingContext);
        }

        protected override object CreateEmptyCollection()
        {
            return new TElement[0];
        }

        /// <inheritdoc />
        protected override object GetModel(IEnumerable<TElement> newCollection)
        {
            return newCollection?.ToArray();
        }

        /// <inheritdoc />
        protected override void CopyToModel([NotNull] object target, IEnumerable<TElement> sourceCollection)
        {
            // Do not attempt to copy values into an array because an array's length is immutable. This choice is also
            // consistent with MutableObjectModelBinder's handling of a read-only array property.
        }
    }
}
