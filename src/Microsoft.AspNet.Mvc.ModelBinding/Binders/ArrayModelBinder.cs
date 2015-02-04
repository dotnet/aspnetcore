// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        public override Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.IsReadOnly)
            {
                return Task.FromResult<ModelBindingResult>(null);
            }

            return base.BindModelAsync(bindingContext);
        }

        protected override object GetModel(IEnumerable<TElement> newCollection)
        {
            return newCollection.ToArray();
        }
    }
}
