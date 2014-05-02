// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            Results = new Dictionary<ModelMetadata, ComplexModelDtoResult>();
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public Collection<ModelMetadata> PropertyMetadata { get; private set; }

        // Contains entries corresponding to each property against which binding was
        // attempted. If binding failed, the entry's value will be null. If binding
        // was never attempted, this dictionary will not contain a corresponding
        // entry.
        public IDictionary<ModelMetadata, ComplexModelDtoResult> Results { get; private set; }
    }
}
