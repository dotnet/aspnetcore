// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // Describes a complex model, but uses a collection rather than individual properties as the data store.
    public class ComplexModelDto
    {
        public ComplexModelDto([NotNull] ModelMetadata modelMetadata,
                               [NotNull] IEnumerable<ModelMetadata> propertyMetadata)
        {
            ModelMetadata = modelMetadata;
            PropertyMetadata = new Collection<ModelMetadata>(propertyMetadata.ToList());
            Results = new Dictionary<ModelMetadata, ModelBindingResult>();
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public Collection<ModelMetadata> PropertyMetadata { get; private set; }

        // Contains entries corresponding to each property against which binding was
        // attempted. If binding failed, the entry's value will be null. If binding
        // was never attempted, this dictionary will not contain a corresponding
        // entry.
        public IDictionary<ModelMetadata, ModelBindingResult> Results { get; private set; }
    }
}
