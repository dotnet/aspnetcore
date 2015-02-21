// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public abstract class AssociatedMetadataProvider<TModelMetadata> : IModelMetadataProvider
        where TModelMetadata : ModelMetadata
    {
        private readonly ConcurrentDictionary<Type, TModelMetadata> _typeInfoCache =
                new ConcurrentDictionary<Type, TModelMetadata>();

        private readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInformation>> _typePropertyInfoCache =
                new ConcurrentDictionary<Type, Dictionary<string, PropertyInformation>>();

        public IEnumerable<ModelMetadata> GetMetadataForProperties(object container, [NotNull] Type containerType)
        {
            return GetMetadataForPropertiesCore(container, containerType);
        }

        public ModelMetadata GetMetadataForProperty(Func<object> modelAccessor,
                                                    [NotNull] Type containerType,
                                                    [NotNull] string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(propertyName));
            }

            var typePropertyInfo = GetTypePropertyInformation(containerType);

            PropertyInformation propertyInfo;
            if (!typePropertyInfo.TryGetValue(propertyName, out propertyInfo))
            {
                var message = Resources.FormatCommon_PropertyNotFound(containerType, propertyName);
                throw new ArgumentException(message, nameof(propertyName));
            }

            return CreatePropertyMetadata(modelAccessor, propertyInfo);
        }

        public ModelMetadata GetMetadataForType(Func<object> modelAccessor, [NotNull] Type modelType)
        {
            var prototype = GetTypeInformation(modelType);
            return CreateMetadataFromPrototype(prototype, modelAccessor);
        }

        public ModelMetadata GetMetadataForParameter(
            Func<object> modelAccessor,
            [NotNull] MethodInfo methodInfo,
            [NotNull] string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(parameterName));
            }

            var parameter = methodInfo.GetParameters().FirstOrDefault(
                param => StringComparer.Ordinal.Equals(param.Name, parameterName));
            if (parameter == null)
            {
                var message = Resources.FormatCommon_ParameterNotFound(parameterName);
                throw new ArgumentException(message, nameof(parameterName));
            }

            return GetMetadataForParameterCore(modelAccessor, parameterName, parameter);
        }

        // Override for creating the prototype metadata (without the model accessor).
        /// <summary>
        /// Creates a new <typeparamref name="TModelMetadata"/> instance.
        /// </summary>
        /// <param name="attributes">The set of attributes relevant for the new instance.</param>
        /// <param name="containerType">
        /// <see cref="Type"/> containing this property. <c>null</c> unless this <typeparamref name="TModelMetadata"/>
        /// describes a property.
        /// </param>
        /// <param name="modelType"><see cref="Type"/> this <typeparamref name="TModelMetadata"/> describes.</param>
        /// <param name="propertyName">
        /// Name of the property (in <paramref name="containerType"/>) or parameter this
        /// <typeparamref name="TModelMetadata"/> describes. <c>null</c> or empty if this
        /// <typeparamref name="TModelMetadata"/> describes a <see cref="Type"/>.
        /// </param>
        /// <returns>A new <typeparamref name="TModelMetadata"/> instance.</returns>
        protected abstract TModelMetadata CreateMetadataPrototype(IEnumerable<object> attributes,
                                                                  Type containerType,
                                                                  Type modelType,
                                                                  string propertyName);

        // Override for applying the prototype + model accessor to yield the final metadata.
        /// <summary>
        /// Creates a new <typeparamref name="TModelMetadata"/> instance based on a <paramref name="prototype"/>.
        /// </summary>
        /// <param name="prototype">
        /// <typeparamref name="TModelMetadata"/> that provides the basis for new instance.
        /// </param>
        /// <param name="modelAccessor">Accessor for model value of new instance.</param>
        /// <returns>
        /// A new <typeparamref name="TModelMetadata"/> instance based on <paramref name="prototype"/>.
        /// </returns>
        protected abstract TModelMetadata CreateMetadataFromPrototype(TModelMetadata prototype,
                                                                      Func<object> modelAccessor);

        private ModelMetadata GetMetadataForParameterCore(Func<object> modelAccessor,
                                                          string parameterName,
                                                          ParameterInfo parameter)
        {
            var parameterInfo =
                CreateParameterInfo(parameter.ParameterType,
                                    ModelAttributes.GetAttributesForParameter(parameter),
                                    parameterName);

            var metadata = CreateMetadataFromPrototype(parameterInfo.Prototype, modelAccessor);
            return metadata;
        }

        private IEnumerable<ModelMetadata> GetMetadataForPropertiesCore(object container, Type containerType)
        {
            var typePropertyInfo = GetTypePropertyInformation(containerType);

            foreach (var kvp in typePropertyInfo)
            {
                var propertyInfo = kvp.Value;
                Func<object> modelAccessor = null;
                if (container != null)
                {
                    modelAccessor = () => propertyInfo.PropertyHelper.GetValue(container);
                }
                var propertyMetadata = CreatePropertyMetadata(modelAccessor, propertyInfo);
                if (propertyMetadata != null)
                {
                    propertyMetadata.Container = container;
                }

                yield return propertyMetadata;
            }
        }

        private TModelMetadata CreatePropertyMetadata(Func<object> modelAccessor, PropertyInformation propertyInfo)
        {
            var metadata = CreateMetadataFromPrototype(propertyInfo.Prototype, modelAccessor);
            if (propertyInfo.IsReadOnly)
            {
                metadata.IsReadOnly = true;
            }

            return metadata;
        }

        private TModelMetadata GetTypeInformation(Type type, IEnumerable<Attribute> associatedAttributes = null)
        {
            // This retrieval is implemented as a TryGetValue/TryAdd instead of a GetOrAdd
            // to avoid the performance cost of creating instance delegates
            TModelMetadata typeInfo;
            if (!_typeInfoCache.TryGetValue(type, out typeInfo))
            {
                typeInfo = CreateTypeInformation(type, associatedAttributes);
                _typeInfoCache.TryAdd(type, typeInfo);
            }

            return typeInfo;
        }

        private Dictionary<string, PropertyInformation> GetTypePropertyInformation(Type type)
        {
            // This retrieval is implemented as a TryGetValue/TryAdd instead of a GetOrAdd
            // to avoid the performance cost of creating instance delegates
            Dictionary<string, PropertyInformation> typePropertyInfo;
            if (!_typePropertyInfoCache.TryGetValue(type, out typePropertyInfo))
            {
                typePropertyInfo = GetPropertiesLookup(type);
                _typePropertyInfoCache.TryAdd(type, typePropertyInfo);
            }

            return typePropertyInfo;
        }

        private TModelMetadata CreateTypeInformation(Type type, IEnumerable<Attribute> associatedAttributes)
        {
            var attributes = ModelAttributes.GetAttributesForType(type);
            if (associatedAttributes != null)
            {
                attributes = attributes.Concat(associatedAttributes);
            }

            return CreateMetadataPrototype(attributes, containerType: null, modelType: type, propertyName: null);
        }

        private PropertyInformation CreatePropertyInformation(Type containerType, PropertyHelper helper)
        {
            var property = helper.Property;
            var attributes = ModelAttributes.GetAttributesForProperty(containerType, property);

            return new PropertyInformation
            {
                PropertyHelper = helper,
                Prototype = CreateMetadataPrototype(attributes,
                                                    containerType,
                                                    property.PropertyType,
                                                    property.Name),
                IsReadOnly = !property.CanWrite || property.SetMethod.IsPrivate
            };
        }

        private Dictionary<string, PropertyInformation> GetPropertiesLookup(Type containerType)
        {
            var properties = new Dictionary<string, PropertyInformation>(StringComparer.Ordinal);
            foreach (var propertyHelper in PropertyHelper.GetProperties(containerType))
            {
                // Avoid re-generating a property descriptor if one has already been generated for the property name
                if (!properties.ContainsKey(propertyHelper.Name))
                {
                    properties.Add(propertyHelper.Name, CreatePropertyInformation(containerType, propertyHelper));
                }
            }

            return properties;
        }

        private ParameterInformation CreateParameterInfo(
            Type parameterType,
            IEnumerable<object> attributes,
            string parameterName)
        {
            var metadataProtoType = CreateMetadataPrototype(attributes: attributes,
                                                    containerType: null,
                                                    modelType: parameterType,
                                                    propertyName: parameterName);

            return new ParameterInformation
            {
                Prototype = metadataProtoType
            };
        }

        private sealed class ParameterInformation
        {
            public TModelMetadata Prototype { get; set; }
        }

        private sealed class PropertyInformation
        {
            public PropertyHelper PropertyHelper { get; set; }
            public TModelMetadata Prototype { get; set; }
            public bool IsReadOnly { get; set; }
        }
    }
}
