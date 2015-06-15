// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Provides access to the  combined list of attributes associated a <see cref="Type"/> or property.
    /// </summary>
    public class ModelAttributes
    {
        /// <summary>
        /// Creates a new <see cref="ModelAttributes"/> for a <see cref="Type"/>.
        /// </summary>
        /// <param name="typeAttributes">The set of attributes for the <see cref="Type"/>.</param>
        public ModelAttributes([NotNull] IEnumerable<object> typeAttributes)
        {
            Attributes = typeAttributes.ToArray();
            TypeAttributes = Attributes;
        }

        /// <summary>
        /// Creates a new <see cref="ModelAttributes"/> for a property.
        /// </summary>
        /// <param name="propertyAttributes">The set of attributes for the property.</param>
        /// <param name="typeAttributes">
        /// The set of attributes for the property's <see cref="Type"/>. See <see cref="PropertyInfo.PropertyType"/>.
        /// </param>
        public ModelAttributes([NotNull] IEnumerable<object> propertyAttributes, [NotNull] IEnumerable<object> typeAttributes)
        {
            PropertyAttributes = propertyAttributes.ToArray();
            TypeAttributes = typeAttributes.ToArray();
            Attributes = PropertyAttributes.Concat(TypeAttributes).ToArray();
        }

        /// <summary>
        /// Gets the set of all attributes. If this instance represents the attributes for a property, the attributes
        /// on the property definition are before those on the property's <see cref="Type"/>.
        /// </summary>
        public IReadOnlyList<object> Attributes { get; }

        /// <summary>
        /// Gets the set of attributes on the property, or <c>null</c> if this instance represents the attributes
        /// for a <see cref="Type"/>.
        /// </summary>
        public IReadOnlyList<object> PropertyAttributes { get; }

        /// <summary>
        /// Gets the set of attributes on the <see cref="Type"/>. If this instance represents a property,
        /// then <see cref="TypeAttributes"/> contains attributes retrieved from
        /// <see cref="PropertyInfo.PropertyType"/>.
        /// </summary>
        public IReadOnlyList<object> TypeAttributes { get; }

        /// <summary>
        /// Gets the attributes for the given <paramref name="property"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> in which caller found <paramref name="property"/>.
        /// </param>
        /// <param name="property">A <see cref="PropertyInfo"/> for which attributes need to be resolved.
        /// </param>
        /// <returns>A <see cref="ModelAttributes"/> instance with the attributes of the property.</returns>
        public static ModelAttributes GetAttributesForProperty([NotNull] Type type, [NotNull] PropertyInfo property)
        {
            var propertyAttributes = property.GetCustomAttributes();
            var typeAttributes = property.PropertyType.GetTypeInfo().GetCustomAttributes();

            var metadataType = GetMetadataType(type);
            if (metadataType != null)
            {
                var metadataProperty = metadataType.GetRuntimeProperty(property.Name);
                if (metadataProperty != null)
                {
                    propertyAttributes = propertyAttributes.Concat(metadataProperty.GetCustomAttributes());
                }
            }

            return new ModelAttributes(propertyAttributes, typeAttributes);
        }

        /// <summary>
        /// Gets the attributes for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which attributes need to be resolved.
        /// </param>
        /// <returns>A <see cref="ModelAttributes"/> instance with the attributes of the <see cref="Type"/>.</returns>
        public static ModelAttributes GetAttributesForType([NotNull] Type type)
        {
            var attributes = type.GetTypeInfo().GetCustomAttributes();

            var metadataType = GetMetadataType(type);
            if (metadataType != null)
            {
                attributes = attributes.Concat(metadataType.GetTypeInfo().GetCustomAttributes());
            }

            return new ModelAttributes(attributes);
        }

        private static Type GetMetadataType(Type type)
        {
            return type.GetTypeInfo().GetCustomAttribute<ModelMetadataTypeAttribute>()?.MetadataType;
        }
    }
}
