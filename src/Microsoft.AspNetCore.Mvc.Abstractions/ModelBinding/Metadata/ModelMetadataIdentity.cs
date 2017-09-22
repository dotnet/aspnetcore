// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A key type which identifies a <see cref="ModelMetadata"/>.
    /// </summary>
    public struct ModelMetadataIdentity : IEquatable<ModelMetadataIdentity>
    {
        /// <summary>
        /// Creates a <see cref="ModelMetadataIdentity"/> for the provided model <see cref="Type"/>.
        /// </summary>
        /// <param name="modelType">The model <see cref="Type"/>.</param>
        /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
        public static ModelMetadataIdentity ForType(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            return new ModelMetadataIdentity()
            {
                ModelType = modelType,
            };
        }

        /// <summary>
        /// Creates a <see cref="ModelMetadataIdentity"/> for the provided property.
        /// </summary>
        /// <param name="modelType">The model type.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="containerType">The container type of the model property.</param>
        /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
        public static ModelMetadataIdentity ForProperty(
            Type modelType,
            string name,
            Type containerType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (containerType == null)
            {
                throw new ArgumentNullException(nameof(containerType));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(name));
            }

            return new ModelMetadataIdentity()
            {
                ModelType = modelType,
                Name = name,
                ContainerType = containerType,
            };
        }

        public static ModelMetadataIdentity ForParameter(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new ModelMetadataIdentity()
            {
                Name = parameter.Name,
                ModelType = parameter.ParameterType,
                ParameterInfo = parameter,
            };
        }

        /// <summary>
        /// Gets the <see cref="Type"/> defining the model property represented by the current
        /// instance, or <c>null</c> if the current instance does not represent a property.
        /// </summary>
        public Type ContainerType { get; private set; }

        /// <summary>
        /// Gets the <see cref="Type"/> represented by the current instance.
        /// </summary>
        public Type ModelType { get; private set; }

        /// <summary>
        /// Gets a value indicating the kind of metadata represented by the current instance.
        /// </summary>
        public ModelMetadataKind MetadataKind
        {
            get
            {
                if (ParameterInfo != null)
                {
                    return ModelMetadataKind.Parameter;
                }
                else if (ContainerType != null && Name != null)
                {
                    return ModelMetadataKind.Property;
                }
                else
                {
                    return ModelMetadataKind.Type;
                }
            }
        }

        /// <summary>
        /// Gets the name of the current instance if it represents a parameter or property, or <c>null</c> if
        /// the current instance represents a type.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a descriptor for the parameter, or <c>null</c> if this instance
        /// does not represent a parameter.
        /// </summary>
        public ParameterInfo ParameterInfo { get; private set; }

        /// <inheritdoc />
        public bool Equals(ModelMetadataIdentity other)
        {
            return
                ContainerType == other.ContainerType &&
                ModelType == other.ModelType &&
                Name == other.Name &&
                ParameterInfo == other.ParameterInfo;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as ModelMetadataIdentity?;
            return other.HasValue && Equals(other.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(ContainerType);
            hash.Add(ModelType);
            hash.Add(Name, StringComparer.Ordinal);
            hash.Add(ParameterInfo);
            return hash;
        }
    }
}