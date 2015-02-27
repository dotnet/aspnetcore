// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default <see cref="ModelMetadata"/> implementation.
    /// </summary>
    public class DefaultModelMetadata : ModelMetadata
    {
        private readonly IModelMetadataProvider _provider;
        private readonly ICompositeMetadataDetailsProvider _detailsProvider;
        private readonly DefaultMetadataDetailsCache _cache;

        private ReadOnlyDictionary<object, object> _additionalValues;
        private bool? _isReadOnly;
        private bool? _isRequired;
        private ModelPropertyCollection _properties;

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadata"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        /// <param name="cache">The <see cref="DefaultMetadataDetailsCache"/>.</param>
        public DefaultModelMetadata(
            [NotNull] IModelMetadataProvider provider,
            [NotNull] ICompositeMetadataDetailsProvider detailsProvider,
            [NotNull] DefaultMetadataDetailsCache cache)
            : base(cache.Key)
        {
            _provider = provider;
            _detailsProvider = detailsProvider;
            _cache = cache;
        }

        /// <summary>
        /// Gets the set of attributes for the current instance.
        /// </summary>
        public IReadOnlyList<object> Attributes
        {
            get
            {
                return _cache.Attributes;
            }
        }

        /// <summary>
        /// Gets the <see cref="Metadata.BindingMetadata"/> for the current instance.
        /// </summary>
        /// <remarks>
        /// Accessing this property will populate the <see cref="Metadata.BindingMetadata"/> if necessary.
        /// </remarks>
        public BindingMetadata BindingMetadata
        {
            get
            {
                if (_cache.BindingMetadata == null)
                {
                    var context = new BindingMetadataProviderContext(Identity, _cache.Attributes);
                    _detailsProvider.GetBindingMetadata(context);
                    _cache.BindingMetadata = context.BindingMetadata;
                }

                return _cache.BindingMetadata;
            }
        }

        /// <summary>
        /// Gets the <see cref="Metadata.DisplayMetadata"/> for the current instance.
        /// </summary>
        /// <remarks>
        /// Accessing this property will populate the <see cref="Metadata.DisplayMetadata"/> if necessary.
        /// </remarks>
        public DisplayMetadata DisplayMetadata
        {
            get
            {
                if (_cache.DisplayMetadata == null)
                {
                    var context = new DisplayMetadataProviderContext(Identity, _cache.Attributes);
                    _detailsProvider.GetDisplayMetadata(context);
                    _cache.DisplayMetadata = context.DisplayMetadata;
                }

                return _cache.DisplayMetadata;
            }
        }

        /// <summary>
        /// Gets the <see cref="Metadata.ValidationMetadata"/> for the current instance.
        /// </summary>
        /// <remarks>
        /// Accessing this property will populate the <see cref="Metadata.ValidationMetadata"/> if necessary.
        /// </remarks>
        public ValidationMetadata ValidationMetadata
        {
            get
            {
                if (_cache.ValidationMetadata == null)
                {
                    var context = new ValidationMetadataProviderContext(Identity, _cache.Attributes);
                    _detailsProvider.GetValidationMetadata(context);
                    _cache.ValidationMetadata = context.ValidationMetadata;
                }

                return _cache.ValidationMetadata;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<object, object> AdditionalValues
        {
            get
            {
                if (_additionalValues == null)
                {
                    _additionalValues = new ReadOnlyDictionary<object, object>(DisplayMetadata.AdditionalValues);
                }

                return _additionalValues;
            }
        }

        /// <inheritdoc />
        public override BindingSource BindingSource
        {
            get
            {
                return BindingMetadata.BindingSource;
            }
        }

        /// <inheritdoc />
        public override string BinderModelName
        {
            get
            {
                return BindingMetadata.BinderModelName;
            }
        }

        /// <inheritdoc />
        public override Type BinderType
        {
            get
            {
                return BindingMetadata.BinderType;
            }
        }

        /// <inheritdoc />
        public override bool ConvertEmptyStringToNull
        {
            get
            {
                return DisplayMetadata.ConvertEmptyStringToNull;
            }
        }

        /// <inheritdoc />
        public override string DataTypeName
        {
            get
            {
                return DisplayMetadata.DataTypeName;
            }
        }

        /// <inheritdoc />
        public override string Description
        {
            get
            {
                return DisplayMetadata.Description;
            }
        }

        /// <inheritdoc />
        public override string DisplayFormatString
        {
            get
            {
                return DisplayMetadata.DisplayFormatString;
            }
        }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                return DisplayMetadata.DisplayName;
            }
        }

        /// <inheritdoc />
        public override string EditFormatString
        {
            get
            {
                return DisplayMetadata.EditFormatString;
            }
        }

        /// <inheritdoc />
        public override bool HasNonDefaultEditFormat
        {
            get
            {
                return DisplayMetadata.HasNonDefaultEditFormat;
            }
        }

        /// <inheritdoc />
        public override bool HideSurroundingHtml
        {
            get
            {
                return DisplayMetadata.HideSurroundingHtml;
            }
        }

        /// <inheritdoc />
        public override bool HtmlEncode
        {
            get
            {
                return DisplayMetadata.HtmlEncode;
            }
        }

        /// <inheritdoc />
        public override bool IsReadOnly
        {
            get
            {
                if (!_isReadOnly.HasValue)
                {
                    if (BindingMetadata.IsReadOnly.HasValue)
                    {
                        _isReadOnly = BindingMetadata.IsReadOnly;
                    }
                    else
                    {
                        _isReadOnly = _cache.PropertySetter != null;
                    }
                }

                return _isReadOnly.Value;
            }
        }

        /// <inheritdoc />
        public override bool IsRequired
        {
            get
            {
                if (!_isRequired.HasValue)
                {
                    if (BindingMetadata.IsRequired.HasValue)
                    {
                        _isRequired = BindingMetadata.IsRequired;
                    }
                    else
                    {
                        _isRequired = !ModelType.AllowsNullValue();
                    }
                }
                
                return _isRequired.Value;
            }
        }

        /// <inheritdoc />
        public override string NullDisplayText
        {
            get
            {
                return DisplayMetadata.NullDisplayText;
            }
        }

        /// <inheritdoc />
        public override int Order
        {
            get
            {
                return DisplayMetadata.Order;
            }
        }

        /// <inheritdoc />
        public override ModelPropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    var properties = _provider.GetMetadataForProperties(ModelType);
                    properties = properties.OrderBy(p => p.Order);
                    _properties = new ModelPropertyCollection(properties);
                }

                return _properties;
            }
        }

        /// <inheritdoc />
        public override IPropertyBindingPredicateProvider PropertyBindingPredicateProvider
        {
            get
            {
                return BindingMetadata.PropertyBindingPredicateProvider;
            }
        }

        /// <inheritdoc />
        public override bool ShowForDisplay
        {
            get
            {
                return DisplayMetadata.ShowForDisplay;
            }
        }

        /// <inheritdoc />
        public override bool ShowForEdit
        {
            get
            {
                return DisplayMetadata.ShowForEdit;
            }
        }

        /// <inheritdoc />
        public override string SimpleDisplayProperty
        {
            get
            {
                return DisplayMetadata.SimpleDisplayProperty;
            }
        }

        /// <inheritdoc />
        public override string TemplateHint
        {
            get
            {
                return DisplayMetadata.TemplateHint;
            }
        }
    }
}