// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Holds associated metadata objects for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    /// <remarks>
    /// Any modifications to the data must be thread-safe for multiple readers and writers.
    /// </remarks>
    public class DefaultMetadataDetails
    {
        /// <summary>
        /// Creates a new <see cref="DefaultMetadataDetails"/>.
        /// </summary>
        /// <param name="key">The <see cref="ModelMetadataIdentity"/>.</param>
        /// <param name="attributes">The set of model attributes.</param>
        public DefaultMetadataDetails(ModelMetadataIdentity key, ModelAttributes attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            Key = key;
            ModelAttributes = attributes;
        }

        /// <summary>
        /// Gets or sets the set of model attributes.
        /// </summary>
        public ModelAttributes ModelAttributes { get; }

        /// <summary>
        /// Gets or sets the <see cref="Metadata.BindingMetadata"/>.
        /// </summary>
        public BindingMetadata? BindingMetadata { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Metadata.DisplayMetadata"/>.
        /// </summary>
        public DisplayMetadata? DisplayMetadata { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadataIdentity"/>.
        /// </summary>
        public ModelMetadataIdentity Key { get; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/> entries for the model properties.
        /// </summary>
        public ModelMetadata[]? Properties { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/> entries for constructor parameters.
        /// </summary>
        public ModelMetadata[]? BoundConstructorParameters { get; set; }

        /// <summary>
        /// Gets or sets a property getter delegate to get the property value from a model object.
        /// </summary>
        public Func<object, object?>? PropertyGetter { get; set; }

        /// <summary>
        /// Gets or sets a property setter delegate to set the property value on a model object.
        /// </summary>
        public Action<object, object?>? PropertySetter { get; set; }

        /// <summary>
        /// Gets or sets a delegate used to invoke the bound constructor for record types.
        /// </summary>
        public Func<object?[], object>? BoundConstructorInvoker { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Metadata.ValidationMetadata"/>
        /// </summary>
        public ValidationMetadata? ValidationMetadata { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/> of the container type.
        /// </summary>
        public ModelMetadata? ContainerMetadata { get; set; }
    }
}
