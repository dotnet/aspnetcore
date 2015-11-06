// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    internal class TestModelValidatorProvider : CompositeModelValidatorProvider
    {
        // Creates a provider with all the defaults - includes data annotations
        public static IModelValidatorProvider CreateDefaultProvider()
        {
            var providers = new IModelValidatorProvider[]
            {
                new DefaultModelValidatorProvider(),
                new DataAnnotationsModelValidatorProvider(
                    new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                    stringLocalizerFactory: null),
            };

            return new TestModelValidatorProvider(providers);
        }

        public TestModelValidatorProvider(IList<IModelValidatorProvider> providers)
            : base(providers)
        {
        }
    }
}