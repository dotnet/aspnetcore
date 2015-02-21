// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Provides static methods which can be used to get a combined list of attributes associated
    /// with a parameter or property.
    /// </summary>
    public static class ModelAttributes
    {
        /// <summary>
        /// Gets the attributes for the given <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">A <see cref="ParameterInfo"/> for which attributes need to be resolved.
        /// </param>
        /// <returns>An <see cref="IEnumerable{object}"/> containing the attributes on the
        /// <paramref name="parameter"/> before the attributes on the <paramref name="parameter"/> type.</returns>
        public static IEnumerable<object> GetAttributesForParameter(ParameterInfo parameter)
        {
            // Return the parameter attributes first.
            var parameterAttributes = parameter.GetCustomAttributes();
            var typeAttributes = parameter.ParameterType.GetTypeInfo().GetCustomAttributes();

            return parameterAttributes.Concat(typeAttributes);
        }

        /// <summary>
        /// Gets the attributes for the given <paramref name="property"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> in which caller found <paramref name="property"/>.
        /// </param>
        /// <param name="property">A <see cref="PropertyInfo"/> for which attributes need to be resolved.
        /// </param>
        /// <returns>An <see cref="IEnumerable{object}"/> containing the attributes on the
        /// <paramref name="property"/> before the attributes on the <paramref name="property"/> type.</returns>
        public static IEnumerable<object> GetAttributesForProperty([NotNull] Type type, [NotNull] PropertyInfo property)
        {
            // Return the property attributes first.
            var propertyAttributes = property.GetCustomAttributes();
            var typeAttributes = property.PropertyType.GetTypeInfo().GetCustomAttributes();

            propertyAttributes = propertyAttributes.Concat(typeAttributes);

            var modelMedatadataType = type.GetTypeInfo().GetCustomAttribute<ModelMetadataTypeAttribute>();
            if (modelMedatadataType != null)
            {
                var modelMedatadataProperty = modelMedatadataType.MetadataType.GetRuntimeProperty(property.Name);
                if (modelMedatadataProperty != null)
                {
                    var modelMedatadataAttributes = modelMedatadataProperty.GetCustomAttributes();
                    propertyAttributes = propertyAttributes.Concat(modelMedatadataAttributes);

                    var modelMetadataTypeAttributes =
                        modelMedatadataProperty.PropertyType.GetTypeInfo().GetCustomAttributes();
                    propertyAttributes = propertyAttributes.Concat(modelMetadataTypeAttributes);
                }
            }

            return propertyAttributes;
        }

        /// <summary>
        /// Gets the attributes for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which attributes need to be resolved.
        /// </param>
        /// <returns>An <see cref="IEnumerable{object}"/> containing the attributes on the
        /// <paramref name="type"/>.</returns>
        public static IEnumerable<object> GetAttributesForType([NotNull] Type type)
        {
            var attributes = type.GetTypeInfo().GetCustomAttributes();

            var modelMedatadataType = type.GetTypeInfo().GetCustomAttribute<ModelMetadataTypeAttribute>();
            if (modelMedatadataType != null)
            {
                var modelMedatadataAttributes = modelMedatadataType.MetadataType.GetTypeInfo().GetCustomAttributes();
                attributes = attributes.Concat(modelMedatadataAttributes);
            }

            return attributes;
        }
    }
}
