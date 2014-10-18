// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
        private bool _isReadOnly;
        private bool _isComplexType;
        private bool _isRequired;
        private bool _showForDisplay;
        private bool _showForEdit;

        private bool _convertEmptyStringToNullComputed;
        private bool _nullDisplayTextComputed;
        private bool _dataTypeNameComputed;
        private bool _descriptionComputed;
        private bool _displayFormatStringComputed;
        private bool _displayNameComputed;
        private bool _editFormatStringComputed;
        private bool _hasNonDefaultEditFormatComputed;
        private bool _hideSurroundingHtmlComputed;
        private bool _isReadOnlyComputed;
        private bool _isComplexTypeComputed;
        private bool _isRequiredComputed;
        private bool _showForDisplayComputed;
        private bool _showForEditComputed;

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
            BinderMetadata = prototype.BinderMetadata;
            IncludedProperties = prototype.IncludedProperties;
            ExcludedProperties = prototype.ExcludedProperties;
            ModelName = prototype.ModelName;
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

        protected TPrototypeCache PrototypeCache { get; set; }

        protected virtual bool ComputeConvertEmptyStringToNull()
        {
            return base.ConvertEmptyStringToNull;
        }

        protected virtual string ComputeNullDisplayText()
        {
            return base.NullDisplayText;
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
