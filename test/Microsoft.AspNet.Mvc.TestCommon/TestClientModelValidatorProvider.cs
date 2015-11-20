// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    internal class TestClientModelValidatorProvider : CompositeClientModelValidatorProvider
    {
        // Creates a provider with all the defaults - includes data annotations
        public static IClientModelValidatorProvider CreateDefaultProvider()
        {
            var providers = new IClientModelValidatorProvider[]
            {
                new DefaultClientModelValidatorProvider(),
                new DataAnnotationsClientModelValidatorProvider(
                    new ValidationAttributeAdapterProvider(),
                    new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                    stringLocalizerFactory: null),
            };

            return new TestClientModelValidatorProvider(providers);
        }

        public TestClientModelValidatorProvider(IEnumerable<IClientModelValidatorProvider> providers)
            : base(providers)
        {
        }
    }
}