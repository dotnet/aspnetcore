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
        private string _description;
        private bool _isReadOnly;
        private bool _isComplexType;
        private bool _isRequired;
        private bool _showForDisplay;
        private bool _showForEdit;

        private bool _convertEmptyStringToNullComputed;
        private bool _nullDisplayTextComputed;
        private bool _descriptionComputed;
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

        protected TPrototypeCache PrototypeCache { get; set; }

        protected virtual bool ComputeConvertEmptyStringToNull()
        {
            return base.ConvertEmptyStringToNull;
        }

        protected virtual string ComputeNullDisplayText()
        {
            return base.NullDisplayText;
        }

        protected virtual string ComputeDescription()
        {
            return base.Description;
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
