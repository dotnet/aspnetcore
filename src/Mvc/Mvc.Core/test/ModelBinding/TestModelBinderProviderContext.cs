// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class TestModelBinderProviderContext : ModelBinderProviderContext
{
    private BindingInfo _bindingInfo;

    // Has to be internal because TestModelMetadataProvider is 'shared' code.
    internal static readonly TestModelMetadataProvider CachedMetadataProvider = new TestModelMetadataProvider();

    private readonly List<Func<ModelMetadata, IModelBinder>> _binderCreators =
        new List<Func<ModelMetadata, IModelBinder>>();

    public TestModelBinderProviderContext(Type modelType)
        : this(modelType, bindingInfo: null)
    {
    }

    public TestModelBinderProviderContext(Type modelType, BindingInfo bindingInfo)
    {
        Metadata = CachedMetadataProvider.GetMetadataForType(modelType);
        MetadataProvider = CachedMetadataProvider;
        _bindingInfo = bindingInfo ?? new BindingInfo
        {
            BinderModelName = Metadata.BinderModelName,
            BinderType = Metadata.BinderType,
            BindingSource = Metadata.BindingSource,
            PropertyFilterProvider = Metadata.PropertyFilterProvider,
        };

        (Services, MvcOptions) = GetServicesAndOptions();
    }

    public TestModelBinderProviderContext(ParameterInfo parameterInfo)
    {
        Metadata = CachedMetadataProvider.GetMetadataForParameter(parameterInfo);
        MetadataProvider = CachedMetadataProvider;
        _bindingInfo = new BindingInfo
        {
            BinderModelName = Metadata.BinderModelName,
            BinderType = Metadata.BinderType,
            BindingSource = Metadata.BindingSource,
            PropertyFilterProvider = Metadata.PropertyFilterProvider,
        };

        (Services, MvcOptions) = GetServicesAndOptions();
    }

    public override BindingInfo BindingInfo => _bindingInfo;

    public override ModelMetadata Metadata { get; }

    public MvcOptions MvcOptions { get; }

    public override IModelMetadataProvider MetadataProvider { get; }

    public override IServiceProvider Services { get; }

    public override IModelBinder CreateBinder(ModelMetadata metadata)
    {
        foreach (var creator in _binderCreators)
        {
            var result = creator(metadata);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public override IModelBinder CreateBinder(ModelMetadata metadata, BindingInfo bindingInfo)
    {
        _bindingInfo = bindingInfo;
        return this.CreateBinder(metadata);
    }

    public void OnCreatingBinder(Func<ModelMetadata, IModelBinder> binderCreator)
    {
        _binderCreators.Add(binderCreator);
    }

    public void OnCreatingBinder(ModelMetadata metadata, Func<IModelBinder> binderCreator)
    {
        _binderCreators.Add((m) => m.Equals(metadata) ? binderCreator() : null);
    }

    private static (IServiceProvider, MvcOptions) GetServicesAndOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

        var mvcOptions = new MvcOptions();
        services.AddSingleton(Options.Create(mvcOptions));

        return (services.BuildServiceProvider(), mvcOptions);
    }
}
