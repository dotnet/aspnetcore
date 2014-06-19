// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadata
    {
        public static readonly int DefaultOrder = 10000;

        private readonly Type _containerType;
        private readonly Type _modelType;
        private readonly string _propertyName;
        private EfficientTypePropertyKey<Type, string> _cacheKey;

        private bool _convertEmptyStringToNull = true;
        private object _model;
        private Func<object> _modelAccessor;
        private int _order = DefaultOrder;
        private IEnumerable<ModelMetadata> _properties;
        private Type _realModelType;
        private string _simpleDisplayText;
        private bool _showForDisplay = true;
        private bool _showForEdit = true;

        public ModelMetadata([NotNull] IModelMetadataProvider provider,
                             Type containerType,
                             Func<object> modelAccessor,
                             [NotNull] Type modelType,
                             string propertyName)
        {
            Provider = provider;

            _containerType = containerType;
            _modelAccessor = modelAccessor;
            _modelType = modelType;
            _propertyName = propertyName;
            IsRequired = !modelType.AllowsNullValue();
        }

        public Type ContainerType
        {
            get { return _containerType; }
        }

        public virtual bool ConvertEmptyStringToNull
        {
            get { return _convertEmptyStringToNull; }
            set { _convertEmptyStringToNull = value; }
        }

        public virtual string DataTypeName { get; set; }

        public virtual string Description { get; set; }

        public virtual string DisplayFormatString { get; set; }

        public virtual string EditFormatString { get; set; }

        public virtual bool IsComplexType
        {
            get { return !ValueProviderResult.CanConvertFromString(ModelType); }
        }

        public bool IsNullableValueType
        {
            get { return ModelType.IsNullableValueType(); }
        }

        public virtual bool IsReadOnly { get; set; }

        public virtual bool IsRequired { get; set; }

        public virtual int Order
        {
            get { return _order; }
            set { _order = value; }
        }

        public object Model
        {
            get
            {
                if (_modelAccessor != null)
                {
                    _model = _modelAccessor();
                    _modelAccessor = null;
                }
                return _model;
            }
            set
            {
                _model = value;
                _modelAccessor = null;
                _properties = null;
                _realModelType = null;
            }
        }

        public Type ModelType
        {
            get { return _modelType; }
        }

        public virtual string NullDisplayText { get; set; }

        public virtual IEnumerable<ModelMetadata> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = Provider.GetMetadataForProperties(Model, RealModelType);
                }
                return _properties;
            }
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        protected IModelMetadataProvider Provider { get; set; }

        /// <returns>
        /// Gets TModel if ModelType is Nullable{TModel}, ModelType otherwise.
        /// </returns>
        public Type RealModelType
        {
            get
            {
                if (_realModelType == null)
                {
                    _realModelType = ModelType;

                    // Don't call GetType() if the model is Nullable<T>, because it will
                    // turn Nullable<T> into T for non-null values
                    if (Model != null && !ModelType.IsNullableValueType())
                    {
                        _realModelType = Model.GetType();
                    }
                }

                return _realModelType;
            }
        }

        public virtual string SimpleDisplayText
        {
            get
            {
                if (_simpleDisplayText == null)
                {
                    _simpleDisplayText = ComputeSimpleDisplayText();
                }

                return _simpleDisplayText;
            }

            set { _simpleDisplayText = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the property should be displayed in read-only views.
        /// </summary>
        public virtual bool ShowForDisplay
        {
            get { return _showForDisplay; }
            set { _showForDisplay = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the property should be displayed in editable views.
        /// </summary>
        public virtual bool ShowForEdit
        {
            get { return _showForEdit; }
            set { _showForEdit = value; }
        }

        public virtual string TemplateHint { get; set; }

        internal EfficientTypePropertyKey<Type, string> CacheKey
        {
            get
            {
                if (_cacheKey == null)
                {
                    _cacheKey = CreateCacheKey(ContainerType, ModelType, PropertyName);
                }

                return _cacheKey;
            }
            set
            {
                _cacheKey = value;
            }
        }

        public virtual string GetDisplayName()
        {
            return PropertyName ?? ModelType.Name;
        }

        protected virtual string ComputeSimpleDisplayText()
        {
            if (Model == null)
            {
                return NullDisplayText;
            }

            var stringResult = Convert.ToString(Model, CultureInfo.CurrentCulture);
            if (stringResult == null)
            {
                return string.Empty;
            }

            if (!stringResult.Equals(Model.GetType().FullName, StringComparison.Ordinal))
            {
                return stringResult;
            }

            var firstProperty = Properties.FirstOrDefault();
            if (firstProperty == null)
            {
                return string.Empty;
            }

            if (firstProperty.Model == null)
            {
                return firstProperty.NullDisplayText;
            }

            return Convert.ToString(firstProperty.Model, CultureInfo.CurrentCulture);
        }

        private static EfficientTypePropertyKey<Type, string> CreateCacheKey(Type containerType,
                                                                             Type modelType,
                                                                             string propertyName)
        {
            // If metadata is for a property then containerType != null && propertyName != null
            // If metadata is for a type then containerType == null && propertyName == null, 
            // so we have to use modelType for the cache key.
            return new EfficientTypePropertyKey<Type, string>(containerType ?? modelType, propertyName);
        }
    }
}
