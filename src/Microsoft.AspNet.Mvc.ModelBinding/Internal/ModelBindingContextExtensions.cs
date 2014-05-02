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
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    internal static class ModelBindingContextExtensions
    {
        public static IEnumerable<IModelValidator> GetValidators([NotNull] this ModelBindingContext context, 
                                                                 [NotNull] ModelMetadata metadata)
        {
            return context.ValidatorProviders.SelectMany(vp => vp.GetValidators(metadata))
                                             .Where(v => v != null);
        }

        public static IEnumerable<ModelValidationResult> Validate([NotNull] this ModelBindingContext bindingContext)
        {
            var validators = GetValidators(bindingContext, bindingContext.ModelMetadata);
            var compositeValidator = new CompositeModelValidator(validators);
            var modelValidationContext = new ModelValidationContext(bindingContext, bindingContext.ModelMetadata);
            return compositeValidator.Validate(modelValidationContext);
        }
    }
}
