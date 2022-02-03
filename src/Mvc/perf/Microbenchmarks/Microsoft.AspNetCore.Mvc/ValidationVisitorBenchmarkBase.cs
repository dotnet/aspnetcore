// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public abstract class ValidationVisitorBenchmarkBase
{
    protected const int Iterations = 4;

    protected static readonly IModelValidatorProvider[] ValidatorProviders = new IModelValidatorProvider[]
    {
            new DefaultModelValidatorProvider(),
            new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                null),
    };

    protected static readonly CompositeModelValidatorProvider CompositeModelValidatorProvider = new CompositeModelValidatorProvider(ValidatorProviders);

    public abstract object Model { get; }

    public ModelMetadataProvider BaselineModelMetadataProvider { get; private set; }
    public ModelMetadataProvider ModelMetadataProvider { get; private set; }
    public ModelMetadata BaselineModelMetadata { get; private set; }
    public ModelMetadata ModelMetadata { get; private set; }
    public ActionContext ActionContext { get; private set; }
    public ValidatorCache ValidatorCache { get; private set; }

    [GlobalSetup]
    public void Setup()
    {
        BaselineModelMetadataProvider = CreateModelMetadataProvider(addHasValidatorsProvider: false);
        ModelMetadataProvider = CreateModelMetadataProvider(addHasValidatorsProvider: true);

        BaselineModelMetadata = BaselineModelMetadataProvider.GetMetadataForType(Model.GetType());
        ModelMetadata = ModelMetadataProvider.GetMetadataForType(Model.GetType());
        ActionContext = GetActionContext();
        ValidatorCache = new ValidatorCache();
    }

    protected static ModelMetadataProvider CreateModelMetadataProvider(bool addHasValidatorsProvider)
    {
        var detailsProviders = new List<IMetadataDetailsProvider>
            {
                new DefaultValidationMetadataProvider(),
            };

        if (addHasValidatorsProvider)
        {
            detailsProviders.Add(new HasValidatorsValidationMetadataProvider(ValidatorProviders));
        }

        var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
        return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
    }

    protected static ActionContext GetActionContext()
    {
        return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    }
}
