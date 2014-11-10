// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CachedDataAnnotationsMetadataAttributes
    {
        public CachedDataAnnotationsMetadataAttributes(IEnumerable<object> attributes)
        {
            DataType = attributes.OfType<DataTypeAttribute>().FirstOrDefault();
            Display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            DisplayColumn = attributes.OfType<DisplayColumnAttribute>().FirstOrDefault();
            DisplayFormat = attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
            Editable = attributes.OfType<EditableAttribute>().FirstOrDefault();
            HiddenInput = attributes.OfType<HiddenInputAttribute>().FirstOrDefault();
            Required = attributes.OfType<RequiredAttribute>().FirstOrDefault();
            ScaffoldColumn = attributes.OfType<ScaffoldColumnAttribute>().FirstOrDefault();
            BinderMetadata = attributes.OfType<IBinderMetadata>().FirstOrDefault();
            PropertyBindingPredicateProviders = attributes.OfType<IPropertyBindingPredicateProvider>();
            BinderModelNameProvider = attributes.OfType<IModelNameProvider>().FirstOrDefault();
            BinderTypeProviders = attributes.OfType<IBinderTypeProviderMetadata>();

            // Special case the [DisplayFormat] attribute hanging off an applied [DataType] attribute. This property is
            // non-null for DataType.Currency, DataType.Date, DataType.Time, and potentially custom [DataType]
            // subclasses. The DataType.Currency, DataType.Date, and DataType.Time [DisplayFormat] attributes have a
            // non-null DataFormatString and the DataType.Date and DataType.Time [DisplayFormat] attributes have
            // ApplyFormatInEditMode==true.
            if (DisplayFormat == null && DataType != null)
            {
                DisplayFormat = DataType.DisplayFormat;
            }
        }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="IEnumerable{IBinderTypeProviderMetadata}"/> found in collection
        /// passed to the <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor,
        /// if any.
        /// </summary>
        public IEnumerable<IBinderTypeProviderMetadata> BinderTypeProviders { get; set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="IBinderMetadata"/> found in collection passed to the
        /// <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor, if any.
        /// </summary>
        public IBinderMetadata BinderMetadata { get; protected set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="IModelNameProvider"/> found in collection passed to the
        /// <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor, if any.
        /// </summary>
        public IModelNameProvider BinderModelNameProvider { get; protected set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="DataTypeAttribute"/> found in collection passed to the
        /// <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor, if any.
        /// </summary>
        public DataTypeAttribute DataType { get; protected set; }

        public DisplayAttribute Display { get; protected set; }

        public DisplayColumnAttribute DisplayColumn { get; protected set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="DisplayFormatAttribute"/> found in collection passed to the
        /// <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor, if any.
        /// If no such attribute was found but a <see cref="DataTypeAttribute"/> was, gets the
        /// <see cref="DataTypeAttribute.DisplayFormat"/> value.
        /// </summary>
        public DisplayFormatAttribute DisplayFormat { get; protected set; }

        public EditableAttribute Editable { get; protected set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="HiddenInputAttribute"/> found in collection passed to the
        /// <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/> constructor, if any.
        /// </summary>
        public HiddenInputAttribute HiddenInput { get; protected set; }

        /// <summary>
        /// Gets (or sets in subclasses) <see cref="IEnumerable{IPropertyBindingPredicateProvider}"/> found in 
        /// collection passed to the <see cref="CachedDataAnnotationsMetadataAttributes(IEnumerable{object})"/>
        /// constructor, if any.
        /// </summary>
        public IEnumerable<IPropertyBindingPredicateProvider> PropertyBindingPredicateProviders { get; protected set; }

        public RequiredAttribute Required { get; protected set; }

        public ScaffoldColumnAttribute ScaffoldColumn { get; protected set; }
    }
}
