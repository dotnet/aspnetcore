// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class TestModelBinderProviderContext : ModelBinderProviderContext
    {
        // Has to be internal because TestModelMetadataProvider is 'shared' code.
        internal static readonly TestModelMetadataProvider CachedMetadataProvider = new TestModelMetadataProvider();

        private readonly List<Func<ModelMetadata, IModelBinder>> _binderCreators =
            new List<Func<ModelMetadata, IModelBinder>>();

        public TestModelBinderProviderContext(Type modelType)
        {
            Metadata = CachedMetadataProvider.GetMetadataForType(modelType);
            MetadataProvider = CachedMetadataProvider;
            BindingInfo = new BindingInfo()
            {
                BinderModelName = Metadata.BinderModelName,
                BinderType = Metadata.BinderType,
                BindingSource = Metadata.BindingSource,
                PropertyFilterProvider = Metadata.PropertyFilterProvider,
            };

        }

        public TestModelBinderProviderContext(ModelMetadata metadata, BindingInfo bindingInfo)
        {
            Metadata = metadata;
            BindingInfo = bindingInfo ?? new BindingInfo
            {
                BinderModelName = metadata.BinderModelName,
                BinderType = metadata.BinderType,
                BindingSource = metadata.BindingSource,
                PropertyFilterProvider = metadata.PropertyFilterProvider,
            };

            MetadataProvider = CachedMetadataProvider;
        }

        public override BindingInfo BindingInfo { get; }

        public override ModelMetadata Metadata { get; }

        public override IModelMetadataProvider MetadataProvider { get; }

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

        public void OnCreatingBinder(Func<ModelMetadata, IModelBinder> binderCreator)
        {
            _binderCreators.Add(binderCreator);
        }

        public void OnCreatingBinder(ModelMetadata metadata, Func<IModelBinder> binderCreator)
        {
            _binderCreators.Add((m) => m.Equals(metadata) ? binderCreator() : null);
        }
    }
}
