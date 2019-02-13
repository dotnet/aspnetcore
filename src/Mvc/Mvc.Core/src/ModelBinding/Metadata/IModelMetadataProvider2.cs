// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    internal interface IModelMetadataProvider2
    {
        /// <summary>
        /// Supplies metadata describing a parameter.
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterInfo"/></param>
        /// <param name="modelType">The actual model type.</param>
        /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="parameter"/>.</returns>
        ModelMetadata GetMetadataForParameter(ParameterInfo parameter, Type modelType);

        /// <summary>
        /// Supplies metadata describing a property.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/>.</param>
        /// <param name="modelType">The actual model type.</param>
        /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="propertyInfo"/>.</returns>
        ModelMetadata GetMetadataForProperty(PropertyInfo propertyInfo, Type modelType);
    }
}
