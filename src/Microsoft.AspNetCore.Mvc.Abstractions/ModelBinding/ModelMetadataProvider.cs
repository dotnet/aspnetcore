// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A provider that can supply instances of <see cref="ModelMetadata"/>.
    /// </summary>
    public abstract class ModelMetadataProvider : IModelMetadataProvider
    {
        /// <summary>
        /// Supplies metadata describing the properties of a <see cref="Type"/>.
        /// </summary>
        /// <param name="modelType">The <see cref="Type"/>.</param>
        /// <returns>A set of <see cref="ModelMetadata"/> instances describing properties of the <see cref="Type"/>.</returns>
        public abstract IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);

        /// <summary>
        /// Supplies metadata describing a <see cref="Type"/>.
        /// </summary>
        /// <param name="modelType">The <see cref="Type"/>.</param>
        /// <returns>A <see cref="ModelMetadata"/> instance describing the <see cref="Type"/>.</returns>
        public abstract ModelMetadata GetMetadataForType(Type modelType);

        /// <summary>
        /// Supplies metadata describing a parameter.
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterInfo"/>.</param>
        /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="parameter"/>.</returns>
        public abstract ModelMetadata GetMetadataForParameter(ParameterInfo parameter);
    }
}
