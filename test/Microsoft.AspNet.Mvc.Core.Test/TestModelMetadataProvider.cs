// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class TestModelMetadataProvider : EmptyModelMetadataProvider
    {
        private List<MetadataBuilder> _builders = new List<MetadataBuilder>();

        protected override ModelMetadata CreateMetadataFromPrototype([NotNull] ModelMetadata prototype)
        {
            var metadata = base.CreateMetadataFromPrototype(prototype);

            if (prototype.PropertyName == null)
            {
                foreach (var builder in _builders)
                {
                    builder.Apply(prototype.ModelType, metadata);
                }
            }
            else
            {
                foreach (var builder in _builders)
                {
                    builder.Apply(prototype.ContainerType, prototype.PropertyName, metadata);
                }
            }

            return metadata;
        }

        public IMetadataBuilder ForType(Type type)
        {
            var builder = new MetadataBuilder(type);
            _builders.Add(builder);
            return builder;
        }

        public IMetadataBuilder ForType<TModel>()
        {
            var builder = new MetadataBuilder(typeof(TModel));
            _builders.Add(builder);
            return builder;
        }

        public IMetadataBuilder ForProperty(Type containerType, string propertyName)
        {
            var builder = new MetadataBuilder(containerType, propertyName);
            _builders.Add(builder);
            return builder;
        }

        public IMetadataBuilder ForProperty<TContainer>(string propertyName)
        {
            var builder = new MetadataBuilder(typeof(TContainer), propertyName);
            _builders.Add(builder);
            return builder;
        }

        public interface IMetadataBuilder
        {
            IMetadataBuilder Then(Action<ModelMetadata> action);
        }

        private class MetadataBuilder : IMetadataBuilder
        {
            private List<Action<ModelMetadata>> _actions = new List<Action<ModelMetadata>>();

            private readonly Type _type;
            private readonly Type _containerType;
            private readonly string _propertyName;

            public MetadataBuilder(Type type)
            {
                _type = type;
            }

            public MetadataBuilder(Type containerType, string propertyName)
            {
                _containerType = containerType;
                _propertyName = propertyName;
            }

            public IMetadataBuilder Then(Action<ModelMetadata> action)
            {
                _actions.Add(action);
                return this;
            }

            public void Apply(Type type, ModelMetadata metadata)
            {
                if (type == _type)
                {
                    foreach (var action in _actions)
                    {
                        action(metadata);
                    }
                }
            }

            public void Apply(Type containerType, string propertyName, ModelMetadata metadata)
            {
                if (containerType == _containerType && propertyName == _propertyName)
                {
                    foreach (var action in _actions)
                    {
                        action(metadata);
                    }
                }
            }
        }
    }
}