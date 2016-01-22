// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Contains data needed for validating a child entry of a model object. See <see cref="IValidationStrategy"/>.
    /// </summary>
    public struct ValidationEntry
    {
        /// <summary>
        /// Creates a new <see cref="ValidationEntry"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> associated with <paramref name="model"/>.</param>
        /// <param name="key">The model prefix associated with <paramref name="model"/>.</param>
        /// <param name="model">The model object.</param>
        public ValidationEntry(ModelMetadata metadata, string key, object model)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Metadata = metadata;
            Key = key;
            Model = model;
        }

        /// <summary>
        /// The model prefix associated with <see cref="Model"/>.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The <see cref="ModelMetadata"/> associated with <see cref="Model"/>.
        /// </summary>
        public ModelMetadata Metadata { get; }

        /// <summary>
        /// The model object.
        /// </summary>
        public object Model { get; }
    }
}
