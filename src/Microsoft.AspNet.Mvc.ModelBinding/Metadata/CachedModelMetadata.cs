// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // This class assumes that model metadata is expensive to create, and allows the user to
    // stash a cache object that can be copied around as a prototype to make creation and
    // computation quicker. It delegates the retrieval of values to getter methods, the results
    // of which are cached on a per-metadata-instance basis.
    //
    // This allows flexible caching strategies: either caching the source of information across
    // instances or caching of the actual information itself, depending on what the developer
    // decides to put into the prototype cache.
    public abstract class CachedModelMetadata<TPrototypeCache> : ModelMetadata
    {
        private bool _convertEmptyStringToNull;
        private string _nullDisplayText;
        private string _dataTypeName;
        private string _description;
        private string _displayFormatString;
        private string _displayName;
        private string _editFormatString;
        private bool _hasNonDefaultEditFormat;
        private bool _hideSurroundingHtml;
        private bool _htmlEncode;
        private bool _isReadOnly;
        private bool _isComplexType;
        private bool _isRequired;
        private bool _showForDisplay;
        private bool _showForEdit;
        private IBinderMetadata _binderMetadata;
        private string _binderModelName;
        private IReadOnlyList<string> _binderIncludeProperties;
        private IReadOnlyList<string> _binderExcludeProperties;
        private Type _binderType;

        private bool _convertEmptyStringToNullComputed;
        private bool _nullDisplayTextComputed;
        private bool _dataTypeNameComputed;
        private bool _descriptionComputed;
        private bool _displayFormatStringComputed;
        private bool _displayNameComputed;
        private bool _editFormatStringComputed;
        private bool _hasNonDefaultEditFormatComputed;
        private bool _hideSurroundingHtmlComputed;
        private bool _htmlEncodeComputed;
        private bool _isReadOnlyComputed;
        private bool _isComplexTypeComputed;
        private bool _isRequiredComputed;
        private bool _showForDisplayComputed;
        private bool _showForEditComputed;
        private bool _isBinderMetadataComputed;
        private bool _isBinderIncludePropertiesComputed;
        private bool _isBinderModelNameComputed;
        private bool _isBinderExcludePropertiesComputed;
        private bool _isBinderTypeComputed;

        // Constructor for creating real instances of the metadata class based on a prototype
        protected CachedModelMetadata(CachedModelMetadata<TPrototypeCache> prototype, Func<object> modelAccessor)
            : base(prototype.Provider,
                   prototype.ContainerType,
                   modelAccessor,
                   prototype.ModelType,
                   prototype.PropertyName)
        {
            CacheKey = prototype.CacheKey;
            PrototypeCache = prototype.PrototypeCache;
            _isComplexType = prototype.IsComplexType;
            _isComplexTypeComputed = true;
        }

        // Constructor for creating the prototype instances of the metadata class
        protected CachedModelMetadata(DataAnnotationsModelMetadataProvider provider,
                                      Type containerType,
                                      Type modelType,
                                      string propertyName,
                                      TPrototypeCache prototypeCache)
            : base(provider, containerType, modelAccessor: null, modelType: modelType, propertyName: propertyName)
        {
            PrototypeCache = prototypeCache;
        }

        /// <inheritdoc />
        public sealed override IBinderMetadata BinderMetadata
        {
            get
            {
                if (!_isBinderMetadataComputed)
                {
                    _binderMetadata = ComputeBinderMetadata();
                    _isBinderMetadataComputed = true;
                }

                return _binderMetadata;
            }

            set
            {
                _binderMetadata = value;
                _isBinderMetadataComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override IReadOnlyList<string> BinderIncludeProperties
        {
            get
            {
                if (!_isBinderIncludePropertiesComputed)
                {
                    _binderIncludeProperties = ComputeBinderIncludeProperties();
                    _isBinderIncludePropertiesComputed = true;
                }

                return _binderIncludeProperties;
            }

            set
            {
                _binderIncludeProperties = value;
                _isBinderIncludePropertiesComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override IReadOnlyList<string> BinderExcludeProperties
        {
            get
            {
                if (!_isBinderExcludePropertiesComputed)
                {
                    _binderExcludeProperties = ComputeBinderExcludeProperties();
                    _isBinderExcludePropertiesComputed = true;
                }

                return _binderExcludeProperties;
            }

            set
            {
                _binderExcludeProperties = value;
                _isBinderExcludePropertiesComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string BinderModelName
        {
            get
            {
                if (!_isBinderModelNameComputed)
                {
                    _binderModelName = ComputeBinderModelNamePrefix();
                    _isBinderModelNameComputed = true;
                }

                return _binderModelName;
            }

            set
            {
                _binderModelName = value;
                _isBinderModelNameComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool ConvertEmptyStringToNull
        {
            get
            {
                if (!_convertEmptyStringToNullComputed)
                {
                    _convertEmptyStringToNull = ComputeConvertEmptyStringToNull();
                    _convertEmptyStringToNullComputed = true;
                }
                return _convertEmptyStringToNull;
            }
            set
            {
                _convertEmptyStringToNull = value;
                _convertEmptyStringToNullComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string DataTypeName
        {
            get
            {
                if (!_dataTypeNameComputed)
                {
                    _dataTypeName = ComputeDataTypeName();
                    _dataTypeNameComputed = true;
                }

                return _dataTypeName;
            }

            set
            {
                _dataTypeName = value;
                _dataTypeNameComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string Description
        {
            get
            {
                if (!_descriptionComputed)
                {
                    _description = ComputeDescription();
                    _descriptionComputed = true;
                }
                return _description;
            }
            set
            {
                _description = value;
                _descriptionComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string DisplayFormatString
        {
            get
            {
                if (!_displayFormatStringComputed)
                {
                    _displayFormatString = ComputeDisplayFormatString();
                    _displayFormatStringComputed = true;
                }

                return _displayFormatString;
            }

            set
            {
                _displayFormatString = value;
                _displayFormatStringComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string DisplayName
        {
            get
            {
                if (!_displayNameComputed)
                {
                    _displayName = ComputeDisplayName();
                    _displayNameComputed = true;
                }

                return _displayName;
            }
            set
            {
                _displayName = value;
                _displayNameComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string EditFormatString
        {
            get
            {
                if (!_editFormatStringComputed)
                {
                    _editFormatString = ComputeEditFormatString();
                    _editFormatStringComputed = true;
                }

                return _editFormatString;
            }

            set
            {
                _editFormatString = value;
                _editFormatStringComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool HasNonDefaultEditFormat
        {
            get
            {
                if (!_hasNonDefaultEditFormatComputed)
                {
                    _hasNonDefaultEditFormat = ComputeHasNonDefaultEditFormat();
                    _hasNonDefaultEditFormatComputed = true;
                }

                return _hasNonDefaultEditFormat;
            }

            set
            {
                _hasNonDefaultEditFormat = value;
                _hasNonDefaultEditFormatComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool HideSurroundingHtml
        {
            get
            {
                if (!_hideSurroundingHtmlComputed)
                {
                    _hideSurroundingHtml = ComputeHideSurroundingHtml();
                    _hideSurroundingHtmlComputed = true;
                }

                return _hideSurroundingHtml;
            }

            set
            {
                _hideSurroundingHtml = value;
                _hideSurroundingHtmlComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool HtmlEncode
        {
            get
            {
                if (!_htmlEncodeComputed)
                {
                    _htmlEncode = ComputeHtmlEncode();
                    _htmlEncodeComputed = true;
                }

                return _htmlEncode;
            }

            set
            {
                _htmlEncode = value;
                _htmlEncodeComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool IsReadOnly
        {
            get
            {
                if (!_isReadOnlyComputed)
                {
                    _isReadOnly = ComputeIsReadOnly();
                    _isReadOnlyComputed = true;
                }
                return _isReadOnly;
            }
            set
            {
                _isReadOnly = value;
                _isReadOnlyComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool IsRequired
        {
            get
            {
                if (!_isRequiredComputed)
                {
                    _isRequired = ComputeIsRequired();
                    _isRequiredComputed = true;
                }
                return _isRequired;
            }
            set
            {
                _isRequired = value;
                _isRequiredComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool IsComplexType
        {
            get
            {
                if (!_isComplexTypeComputed)
                {
                    _isComplexType = ComputeIsComplexType();
                    _isComplexTypeComputed = true;
                }
                return _isComplexType;
            }
        }

        /// <inheritdoc />
        public sealed override string NullDisplayText
        {
            get
            {
                if (!_nullDisplayTextComputed)
                {
                    _nullDisplayText = ComputeNullDisplayText();
                    _nullDisplayTextComputed = true;
                }
                return _nullDisplayText;
            }
            set
            {
                _nullDisplayText = value;
                _nullDisplayTextComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool ShowForDisplay
        {
            get
            {
                if (!_showForDisplayComputed)
                {
                    _showForDisplay = ComputeShowForDisplay();
                    _showForDisplayComputed = true;
                }
                return _showForDisplay;
            }
            set
            {
                _showForDisplay = value;
                _showForDisplayComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override bool ShowForEdit
        {
            get
            {
                if (!_showForEditComputed)
                {
                    _showForEdit = ComputeShowForEdit();
                    _showForEditComputed = true;
                }
                return _showForEdit;
            }
            set
            {
                _showForEdit = value;
                _showForEditComputed = true;
            }
        }

        /// <inheritdoc />
        public sealed override string SimpleDisplayText
        {
            get
            {
                // Value already cached in ModelMetadata. That class also already exposes ComputeSimpleDisplayText()
                // for overrides. Sealed here for consistency with other properties.
                return base.SimpleDisplayText;
            }
            set
            {
                base.SimpleDisplayText = value;
            }
        }

        /// <inheritdoc />
        public sealed override Type BinderType
        {
            get
            {
                if (!_isBinderTypeComputed)
                {
                    _binderType = ComputeBinderType();
                    _isBinderTypeComputed = true;
                }
                return _binderType;
            }
            set
            {
                _binderType = value;
                _isBinderTypeComputed = true;
            }
        }

        protected TPrototypeCache PrototypeCache { get; set; }

        protected virtual Type ComputeBinderType()
        {
            return base.BinderType;
        }

        protected virtual IBinderMetadata ComputeBinderMetadata()
        {
            return base.BinderMetadata;
        }

        protected virtual IReadOnlyList<string> ComputeBinderIncludeProperties()
        {
            return base.BinderIncludeProperties;
        }

        protected virtual IReadOnlyList<string> ComputeBinderExcludeProperties()
        {
            return base.BinderExcludeProperties;
        }

        protected virtual string ComputeBinderModelNamePrefix()
        {
            return base.BinderModelName;
        }

        protected virtual bool ComputeConvertEmptyStringToNull()
        {
            return base.ConvertEmptyStringToNull;
        }

        /// <summary>
        /// Calculate the <see cref="DataTypeName"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="DataTypeName"/> value.</returns>
        protected virtual string ComputeDataTypeName()
        {
            return base.DataTypeName;
        }

        protected virtual string ComputeDescription()
        {
            return base.Description;
        }

        /// <summary>
        /// Calculate the <see cref="DisplayFormatString"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="DisplayFormatString"/> value.</returns>
        protected virtual string ComputeDisplayFormatString()
        {
            return base.DisplayFormatString;
        }

        protected virtual string ComputeDisplayName()
        {
            return base.DisplayName;
        }

        /// <summary>
        /// Calculate the <see cref="EditFormatString"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="EditFormatString"/> value.</returns>
        protected virtual string ComputeEditFormatString()
        {
            return base.EditFormatString;
        }

        /// <summary>
        /// Calculate the <see cref="HasNonDefaultEditFormat"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="HasNonDefaultEditFormat"/> value.</returns>
        protected virtual bool ComputeHasNonDefaultEditFormat()
        {
            return base.HasNonDefaultEditFormat;
        }

        /// <summary>
        /// Calculate the <see cref="HideSurroundingHtml"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="HideSurroundingHtml"/> value.</returns>
        protected virtual bool ComputeHideSurroundingHtml()
        {
            return base.HideSurroundingHtml;
        }

        /// <summary>
        /// Calculate the <see cref="HtmlEncode"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="HtmlEncode"/> value.</returns>
        protected virtual bool ComputeHtmlEncode()
        {
            return base.HtmlEncode;
        }

        protected virtual bool ComputeIsReadOnly()
        {
            return base.IsReadOnly;
        }

        protected virtual bool ComputeIsRequired()
        {
            return base.IsRequired;
        }

        protected virtual bool ComputeIsComplexType()
        {
            return base.IsComplexType;
        }

        protected virtual string ComputeNullDisplayText()
        {
            return base.NullDisplayText;
        }

        protected virtual bool ComputeShowForDisplay()
        {
            return base.ShowForDisplay;
        }

        protected virtual bool ComputeShowForEdit()
        {
            return base.ShowForEdit;
        }
    }
}
