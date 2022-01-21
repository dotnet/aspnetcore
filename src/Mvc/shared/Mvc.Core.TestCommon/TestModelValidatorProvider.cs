// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
