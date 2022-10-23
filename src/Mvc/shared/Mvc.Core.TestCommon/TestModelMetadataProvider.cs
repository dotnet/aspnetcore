// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class TestModelMetadataProvider : DefaultModelMetadataProvider
{
    private static DataAnnotationsMetadataProvider CreateDefaultDataAnnotationsProvider(IStringLocalizerFactory stringLocalizerFactory)
    {
        var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());
        localizationOptions.Value.DataAnnotationLocalizerProvider = (modelType, localizerFactory) => localizerFactory.Create(modelType);

        return new DataAnnotationsMetadataProvider(new MvcOptions(), localizationOptions, stringLocalizerFactory);
    }

    // Creates a provider with all the defaults - includes data annotations
    public static ModelMetadataProvider CreateDefaultProvider(IStringLocalizerFactory stringLocalizerFactory = null)
    {
        var detailsProviders = new List<IMetadataDetailsProvider>
            {
                new DefaultBindingMetadataProvider(),
                new DefaultValidationMetadataProvider(),
                CreateDefaultDataAnnotationsProvider(stringLocalizerFactory),
                new DataMemberRequiredBindingMetadataProvider(),
            };

        MvcCoreMvcOptionsSetup.ConfigureAdditionalModelMetadataDetailsProviders(detailsProviders);

        var validationProviders = TestModelValidatorProvider.CreateDefaultProvider();
        detailsProviders.Add(new HasValidatorsValidationMetadataProvider(validationProviders.ValidatorProviders));

        var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
        return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
    }

    public static IModelMetadataProvider CreateDefaultProvider(IList<IMetadataDetailsProvider> providers)
    {
        var detailsProviders = new List<IMetadataDetailsProvider>()
            {
                new DefaultBindingMetadataProvider(),
                new DefaultValidationMetadataProvider(),
                new DataAnnotationsMetadataProvider(
                    new MvcOptions(),
                    Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                    stringLocalizerFactory: null),
                new DataMemberRequiredBindingMetadataProvider(),
            };

        MvcCoreMvcOptionsSetup.ConfigureAdditionalModelMetadataDetailsProviders(detailsProviders);

        detailsProviders.AddRange(providers);

        var validationProviders = TestModelValidatorProvider.CreateDefaultProvider();
        detailsProviders.Add(new HasValidatorsValidationMetadataProvider(validationProviders.ValidatorProviders));

        var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
        return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
    }

    public static IModelMetadataProvider CreateProvider(IList<IMetadataDetailsProvider> providers)
    {
        var detailsProviders = new List<IMetadataDetailsProvider>();
        if (providers != null)
        {
            detailsProviders.AddRange(providers);
        }

        var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
        return new DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new MvcOptions()));
    }

    private readonly TestModelMetadataDetailsProvider _detailsProvider;

    public TestModelMetadataProvider()
        : this(new TestModelMetadataDetailsProvider())
    {
    }

    private TestModelMetadataProvider(TestModelMetadataDetailsProvider detailsProvider)
        : base(
              new DefaultCompositeMetadataDetailsProvider(new IMetadataDetailsProvider[]
              {
                      new DefaultBindingMetadataProvider(),
                      new DefaultValidationMetadataProvider(),
                      new DataAnnotationsMetadataProvider(
                          new MvcOptions(),
                          Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                          stringLocalizerFactory: null),
                      detailsProvider
              }),
              Options.Create(new MvcOptions()))
    {
        _detailsProvider = detailsProvider;
    }

    public IMetadataBuilder ForType(Type type)
    {
        var key = ModelMetadataIdentity.ForType(type);

        var builder = new MetadataBuilder(key);
        _detailsProvider.Builders.Add(builder);
        return builder;
    }

    public IMetadataBuilder ForType<TModel>()
    {
        return ForType(typeof(TModel));
    }

    public IMetadataBuilder ForProperty(Type containerType, string propertyName)
    {
        var property = containerType.GetRuntimeProperty(propertyName);
        Assert.NotNull(property);

        var key = ModelMetadataIdentity.ForProperty(property, property.PropertyType, containerType);

        var builder = new MetadataBuilder(key);
        _detailsProvider.Builders.Add(builder);
        return builder;
    }

    public IMetadataBuilder ForParameter(ParameterInfo parameter)
    {
        var key = ModelMetadataIdentity.ForParameter(parameter);
        var builder = new MetadataBuilder(key);
        _detailsProvider.Builders.Add(builder);

        return builder;
    }

    public IMetadataBuilder ForProperty<TContainer>(string propertyName)
    {
        return ForProperty(typeof(TContainer), propertyName);
    }

    private sealed class TestModelMetadataDetailsProvider :
        IBindingMetadataProvider,
        IDisplayMetadataProvider,
        IValidationMetadataProvider
    {
        public List<MetadataBuilder> Builders { get; } = new List<MetadataBuilder>();

        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            foreach (var builder in Builders)
            {
                builder.Apply(context);
            }
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            foreach (var builder in Builders)
            {
                builder.Apply(context);
            }
        }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var builder in Builders)
            {
                builder.Apply(context);
            }
        }
    }

    public interface IMetadataBuilder
    {
        IMetadataBuilder BindingDetails(Action<BindingMetadata> action);

        IMetadataBuilder DisplayDetails(Action<DisplayMetadata> action);

        IMetadataBuilder ValidationDetails(Action<ValidationMetadata> action);
    }

    private sealed class MetadataBuilder : IMetadataBuilder
    {
        private readonly List<Action<BindingMetadata>> _bindingActions = new List<Action<BindingMetadata>>();
        private readonly List<Action<DisplayMetadata>> _displayActions = new List<Action<DisplayMetadata>>();
        private readonly List<Action<ValidationMetadata>> _validationActions = new List<Action<ValidationMetadata>>();

        private readonly ModelMetadataIdentity _key;

        public MetadataBuilder(ModelMetadataIdentity key)
        {
            _key = key;
        }

        public void Apply(BindingMetadataProviderContext context)
        {
            if (_key.Equals(context.Key))
            {
                foreach (var action in _bindingActions)
                {
                    action(context.BindingMetadata);
                }
            }
        }

        public void Apply(DisplayMetadataProviderContext context)
        {
            if (_key.Equals(context.Key))
            {
                foreach (var action in _displayActions)
                {
                    action(context.DisplayMetadata);
                }
            }
        }

        public void Apply(ValidationMetadataProviderContext context)
        {
            if (_key.Equals(context.Key))
            {
                foreach (var action in _validationActions)
                {
                    action(context.ValidationMetadata);
                }
            }
        }

        public IMetadataBuilder BindingDetails(Action<BindingMetadata> action)
        {
            _bindingActions.Add(action);
            return this;
        }

        public IMetadataBuilder DisplayDetails(Action<DisplayMetadata> action)
        {
            _displayActions.Add(action);
            return this;
        }

        public IMetadataBuilder ValidationDetails(Action<ValidationMetadata> action)
        {
            _validationActions.Add(action);
            return this;
        }
    }
}
