// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class TestModelValidatorProvider : CompositeModelValidatorProvider
    {
        // Creates a provider with all the defaults - includes data annotations
        public static CompositeModelValidatorProvider CreateDefaultProvider(IStringLocalizerFactory stringLocalizerFactory = null)
        {
            var options = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            options.Value.DataAnnotationLocalizerProvider = (modelType, localizerFactory) => localizerFactory.Create(modelType);

            var providers = new IModelValidatorProvider[]
            {
                new DefaultModelValidatorProvider(),
                new DataAnnotationsModelValidatorProvider(
                    new ValidationAttributeAdapterProvider(),
                    options,
                    stringLocalizerFactory)
            };

            return new TestModelValidatorProvider(providers);
        }

        public TestModelValidatorProvider(IList<IModelValidatorProvider> providers)
            : base(providers)
        {
        }
    }
}
