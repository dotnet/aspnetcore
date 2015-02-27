// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Binding metadata details for a <see cref="ModelMetadata"/>.
    /// </summary>
    public class BindingMetadata
    {
        /// <summary>
        /// Gets or sets the <see cref="ModelBinding.BindingSource"/>.
        /// See <see cref="ModelMetadata.BindingSource"/>.
        /// </summary>
        public BindingSource BindingSource { get; set; }

        /// <summary>
        /// Gets or sets the binder model name. If <c>null</c> the property or parameter name will be used.
        /// See <see cref="ModelMetadata.BinderModelName"/>.
        /// </summary>
        public string BinderModelName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> of the model binder used to bind the model.
        /// See <see cref="ModelMetadata.BinderType"/>.
        /// </summary>
        public Type BinderType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the model is read-only. Will be ignored
        /// if the model metadata being created is not a property. If <c>null</c> then
        /// <see cref="ModelMetadata.IsReadOnly"/> will be  computed based on the accessibility
        /// of the property accessor and model <see cref="Type"/>. See <see cref="ModelMetadata.IsReadOnly"/>.
        /// </summary>
        public bool? IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the model is a required value. Will be ignored
        /// if the model metadata being created is not a property. If <c>null</c> then
        /// <see cref="ModelMetadata.IsRequired"/> will be computed based on the model <see cref="Type"/>.
        /// See <see cref="ModelMetadata.IsRequired"/>.
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelBinding.IPropertyBindingPredicateProvider"/>.
        /// See <see cref="ModelMetadata.PropertyBindingPredicateProvider"/>.
        /// </summary>
        public IPropertyBindingPredicateProvider PropertyBindingPredicateProvider { get; set; }
    }
}