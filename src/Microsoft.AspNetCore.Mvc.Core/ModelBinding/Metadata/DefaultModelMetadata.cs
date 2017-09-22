// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default <see cref="ModelMetadata"/> implementation.
    /// </summary>
    public class DefaultModelMetadata : ModelMetadata
    {
        private readonly IModelMetadataProvider _provider;
        private readonly ICompositeMetadataDetailsProvider _detailsProvider;
        private readonly DefaultMetadataDetails _details;

        // Default message provider for all DefaultModelMetadata instances; cloned before exposing to
        // IBindingMetadataProvider instances to ensure customizations are not accidentally shared.
        private readonly DefaultModelBindingMessageProvider _modelBindingMessageProvider;

        private ReadOnlyDictionary<object, object> _additionalValues;
        private ModelMetadata _elementMetadata;
        private bool? _isBindingRequired;
        private bool? _isReadOnly;
        private bool? _isRequired;
        private ModelPropertyCollection _properties;
        private bool? _validateChildren;
        private ReadOnlyCollection<object> _validatorMetadata;

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadata"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        /// <param name="details">The <see cref="DefaultMetadataDetails"/>.</param>
        public DefaultModelMetadata(
            IModelMetadataProvider provider,
            ICompositeMetadataDetailsProvider detailsProvider,
            DefaultMetadataDetails details)
            : this(provider, detailsProvider, details, new DefaultModelBindingMessageProvider())
        {
        }

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadata"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        /// <param name="details">The <see cref="DefaultMetadataDetails"/>.</param>
        /// <param name="modelBindingMessageProvider">The <see cref="Metadata.DefaultModelBindingMessageProvider"/>.</param>
        public DefaultModelMetadata(
            IModelMetadataProvider provider,
            ICompositeMetadataDetailsProvider detailsProvider,
            DefaultMetadataDetails details,
            DefaultModelBindingMessageProvider modelBindingMessageProvider)
            : base(details.Key)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (detailsProvider == null)
            {
                throw new ArgumentNullException(nameof(detailsProvider));
            }

            if (details == null)
            {
                throw new ArgumentNullException(nameof(details));
            }

            if (modelBindingMessageProvider == null)
            {
                throw new ArgumentNullException(nameof(modelBindingMessageProvider));
            }

            _provider = provider;
            _detailsProvider = detailsProvider;
            _details = details;
            _modelBindingMessageProvider = modelBindingMessageProvider;
        }

        /// <summary>
        /// Gets the set of attributes for the current instance.
        /// </summary>
        public ModelAttributes Attributes => _details.ModelAttributes;

        /// <inheritdoc />
        public override ModelMetadata ContainerMetadata => _details.ContainerMetadata;

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
                if (_details.BindingMetadata == null)
                {
                    var context = new BindingMetadataProviderContext(Identity, _details.ModelAttributes);

                    // Provide a unique ModelBindingMessageProvider instance so providers' customizations are per-type.
                    context.BindingMetadata.ModelBindingMessageProvider =
                        new DefaultModelBindingMessageProvider(_modelBindingMessageProvider);

                    _detailsProvider.CreateBindingMetadata(context);
                    _details.BindingMetadata = context.BindingMetadata;
                }

                return _details.BindingMetadata;
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
                if (_details.DisplayMetadata == null)
                {
                    var context = new DisplayMetadataProviderContext(Identity, _details.ModelAttributes);
                    _detailsProvider.CreateDisplayMetadata(context);
                    _details.DisplayMetadata = context.DisplayMetadata;
                }

                return _details.DisplayMetadata;
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
                if (_details.ValidationMetadata == null)
                {
                    var context = new ValidationMetadataProviderContext(Identity, _details.ModelAttributes);
                    _detailsProvider.CreateValidationMetadata(context);
                    _details.ValidationMetadata = context.ValidationMetadata;
                }

                return _details.ValidationMetadata;
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
        public override BindingSource BindingSource => BindingMetadata.BindingSource;

        /// <inheritdoc />
        public override string BinderModelName => BindingMetadata.BinderModelName;

        /// <inheritdoc />
        public override Type BinderType => BindingMetadata.BinderType;

        /// <inheritdoc />
        public override bool ConvertEmptyStringToNull => DisplayMetadata.ConvertEmptyStringToNull;

        /// <inheritdoc />
        public override string DataTypeName => DisplayMetadata.DataTypeName;

        /// <inheritdoc />
        public override string Description
        {
            get
            {
                if (DisplayMetadata.Description == null)
                {
                    return null;
                }

                return DisplayMetadata.Description();
            }
        }

        /// <inheritdoc />
        public override string DisplayFormatString => DisplayMetadata.DisplayFormatString;

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                if (DisplayMetadata.DisplayName == null)
                {
                    return null;
                }

                return DisplayMetadata.DisplayName();
            }
        }

        /// <inheritdoc />
        public override string EditFormatString => DisplayMetadata.EditFormatString;

        /// <inheritdoc />
        public override ModelMetadata ElementMetadata
        {
            get
            {
                if (_elementMetadata == null && ElementType != null)
                {
                    _elementMetadata = _provider.GetMetadataForType(ElementType);
                }

                return _elementMetadata;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues
            => DisplayMetadata.EnumGroupedDisplayNamesAndValues;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, string> EnumNamesAndValues => DisplayMetadata.EnumNamesAndValues;

        /// <inheritdoc />
        public override bool HasNonDefaultEditFormat => DisplayMetadata.HasNonDefaultEditFormat;

        /// <inheritdoc />
        public override bool HideSurroundingHtml => DisplayMetadata.HideSurroundingHtml;

        /// <inheritdoc />
        public override bool HtmlEncode => DisplayMetadata.HtmlEncode;

        /// <inheritdoc />
        public override bool IsBindingAllowed
        {
            get
            {
                if (MetadataKind == ModelMetadataKind.Type)
                {
                    return true;
                }
                else
                {
                    return BindingMetadata.IsBindingAllowed;
                }
            }
        }

        /// <inheritdoc />
        public override bool IsBindingRequired
        {
            get
            {
                if (!_isBindingRequired.HasValue)
                {
                    if (MetadataKind == ModelMetadataKind.Type)
                    {
                        _isBindingRequired = false;
                    }
                    else
                    {
                        _isBindingRequired = BindingMetadata.IsBindingRequired;
                    }
                }

                return _isBindingRequired.Value;
            }
        }

        /// <inheritdoc />
        public override bool IsEnum => DisplayMetadata.IsEnum;

        /// <inheritdoc />
        public override bool IsFlagsEnum => DisplayMetadata.IsFlagsEnum;

        /// <inheritdoc />
        public override bool IsReadOnly
        {
            get
            {
                if (!_isReadOnly.HasValue)
                {
                    if (MetadataKind == ModelMetadataKind.Type)
                    {
                        _isReadOnly = false;
                    }
                    else if (BindingMetadata.IsReadOnly.HasValue)
                    {
                        _isReadOnly = BindingMetadata.IsReadOnly;
                    }
                    else
                    {
                        _isReadOnly = _details.PropertySetter == null;
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
                    if (ValidationMetadata.IsRequired.HasValue)
                    {
                        _isRequired = ValidationMetadata.IsRequired;
                    }
                    else
                    {
                        // Default to IsRequired = true for non-Nullable<T> value types.
                        _isRequired = !IsReferenceOrNullableType;
                    }
                }

                return _isRequired.Value;
            }
        }

        /// <inheritdoc />
        public override ModelBindingMessageProvider ModelBindingMessageProvider =>
            BindingMetadata.ModelBindingMessageProvider;

        /// <inheritdoc />
        public override string NullDisplayText => DisplayMetadata.NullDisplayText;

        /// <inheritdoc />
        public override int Order => DisplayMetadata.Order;

        /// <inheritdoc />
        public override string Placeholder
        {
            get
            {
                if (DisplayMetadata.Placeholder == null)
                {
                    return null;
                }

                return DisplayMetadata.Placeholder();
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
        public override IPropertyFilterProvider PropertyFilterProvider => BindingMetadata.PropertyFilterProvider;

        /// <inheritdoc />
        public override bool ShowForDisplay => DisplayMetadata.ShowForDisplay;

        /// <inheritdoc />
        public override bool ShowForEdit => DisplayMetadata.ShowForEdit;

        /// <inheritdoc />
        public override string SimpleDisplayProperty => DisplayMetadata.SimpleDisplayProperty;

        /// <inheritdoc />
        public override string TemplateHint => DisplayMetadata.TemplateHint;

        /// <inheritdoc />
        public override IPropertyValidationFilter PropertyValidationFilter => ValidationMetadata.PropertyValidationFilter;

        /// <inheritdoc />
        public override bool ValidateChildren
        {
            get
            {
                if (!_validateChildren.HasValue)
                {
                    if (ValidationMetadata.ValidateChildren.HasValue)
                    {
                        _validateChildren = ValidationMetadata.ValidateChildren.Value;
                    }
                    else if (IsComplexType || IsEnumerableType)
                    {
                        _validateChildren = true;
                    }
                    else
                    {
                        _validateChildren = false;
                    }
                }

                return _validateChildren.Value;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<object> ValidatorMetadata
        {
            get
            {
                if (_validatorMetadata == null)
                {
                    _validatorMetadata = new ReadOnlyCollection<object>(ValidationMetadata.ValidatorMetadata);
                }

                return _validatorMetadata;
            }
        }

        /// <inheritdoc />
        public override Func<object, object> PropertyGetter => _details.PropertyGetter;

        /// <inheritdoc />
        public override Action<object, object> PropertySetter => _details.PropertySetter;

        /// <inheritdoc />
        public override ModelMetadata GetMetadataForType(Type modelType)
        {
            return _provider.GetMetadataForType(modelType);
        }

        /// <inheritdoc />
        public override IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType)
        {
            return _provider.GetMetadataForProperties(modelType);
        }
    }
}