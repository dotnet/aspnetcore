// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "The method is a delegating helper to choose among multiple property values")]
        public virtual string GetDisplayName()
        {
            return PropertyName ?? ModelType.Name;
        }

        // TODO: Revive ModelValidators
        //public virtual IEnumerable<ModelValidator> GetValidators(IEnumerable<ModelValidatorProvider> validatorProviders)
        //{
        //    if (validatorProviders == null)
        //    {
        //        throw Error.ArgumentNull("validatorProviders");
        //    }

        //    return validatorProviders.SelectMany(provider => provider.GetValidators(this, validatorProviders));
        //}

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

        private static EfficientTypePropertyKey<Type, string> CreateCacheKey(Type containerType, Type modelType, string propertyName)
        {
            // If metadata is for a property then containerType != null && propertyName != null
            // If metadata is for a type then containerType == null && propertyName == null, so we have to use modelType for the cache key.
            return new EfficientTypePropertyKey<Type, string>(containerType ?? modelType, propertyName);
        }
    }
}
