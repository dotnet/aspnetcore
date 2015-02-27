// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A key type which identifies a <see cref="ModelMetadata"/>.
    /// </summary>
    public struct ModelMetadataIdentity
    {
        /// <summary>
        /// Creates a <see cref="ModelMetadataIdentity"/> for the provided model <see cref="Type"/>.
        /// </summary>
        /// <param name="modelType">The model <see cref="Type"/>.</param>
        /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
        public static ModelMetadataIdentity ForType([NotNull] Type modelType)
        {
            return new ModelMetadataIdentity()
            {
                ModelType = modelType,
            };
        }

        /// <summary>
        /// Creates a <see cref="ModelMetadataIdentity"/> for the provided <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="parameterInfo">The model parameter.</param>
        /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
        public static ModelMetadataIdentity ForParameter([NotNull] ParameterInfo parameterInfo)
        {
            return new ModelMetadataIdentity()
            {
                ParameterInfo = parameterInfo,
                Name = parameterInfo.Name,
                ModelType = parameterInfo.ParameterType,
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
            [NotNull] Type modelType,
            string name,
            [NotNull] Type containerType)
        {
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

        /// <summary>
        /// Gets the <see cref="Type"/> defining the model property respresented by the current
        /// instance, or <c>null</c> if the current instance does not represent a property.
        /// </summary>
        public Type ContainerType { get; private set; }

        /// <summary>
        /// Gets the <see cref="ParameterInfo"/> represented by the current instance, or <c>null</c>
        /// if the current instance does not represent a parameter.
        /// </summary>
        public ParameterInfo ParameterInfo { get; private set; }

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
    }
}