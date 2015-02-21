// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadata
    {
        public static readonly int DefaultOrder = 10000;

        private readonly Type _containerType;
        private readonly Type _modelType;
        private readonly string _propertyName;
        private EfficientTypePropertyKey<Type, string> _cacheKey;

        // Backing fields for virtual properties with default values.
        private bool _convertEmptyStringToNull = true;
        private bool _htmlEncode = true;
        private bool _showForDisplay = true;
        private bool _showForEdit = true;

        private object _model;
        private Func<object> _modelAccessor;
        private int _order = DefaultOrder;
        private bool _isRequired;
        private ModelPropertyCollection _properties;
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
            _isRequired = !modelType.AllowsNullValue();
        }

        /// <summary>
        /// Gets a collection of additional information about the model.
        /// </summary>
        public virtual IDictionary<object, object> AdditionalValues { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Gets or sets the name of a model if specified explicitly using <see cref="IModelNameProvider"/>.
        /// </summary>
        public virtual string BinderModelName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> of an <see cref="IModelBinder"/> or an
        /// <see cref="IModelBinderProvider"/> of a model if specified explicitly using 
        /// <see cref="IBinderTypeProviderMetadata"/>.
        /// </summary>
        public virtual Type BinderType { get; set; }

        /// <summary>
        /// Gets or sets a binder metadata for this model.
        /// </summary>
        public virtual IBinderMetadata BinderMetadata { get; set; }

        /// <summary>
        /// A reference to the model's container <see cref="object"/>.
        /// Will be non-<c>null</c> if the model represents a property.
        /// </summary>
        public object Container { get; set; }

        public Type ContainerType
        {
            get { return _containerType; }
        }

        public virtual bool ConvertEmptyStringToNull
        {
            get { return _convertEmptyStringToNull; }
            set { _convertEmptyStringToNull = value; }
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="Model"/>'s datatype.  Overrides <see cref="ModelType"/> in some
        /// display scenarios.
        /// </summary>
        /// <value><c>null</c> unless set manually or through additional metadata e.g. attributes.</value>
        public virtual string DataTypeName { get; set; }

        public virtual string Description { get; set; }

        /// <summary>
        /// Gets or sets the composite format <see cref="string"/> (see
        /// http://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to display the <see cref="Model"/>.
        /// </summary>
        public virtual string DisplayFormatString { get; set; }

        public virtual string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the composite format <see cref="string"/> (see
        /// http://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to edit the <see cref="Model"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="IModelMetadataProvider"/> instances that set this property to a non-<c>null</c>, non-empty,
        /// non-default value should also set <see cref="HasNonDefaultEditFormat"/> to <c>true</c>.
        /// </remarks>
        public virtual string EditFormatString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="EditFormatString"/> has a non-<c>null</c>, non-empty
        /// value different from the default for the datatype.
        /// </summary>
        public virtual bool HasNonDefaultEditFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value should be HTML-encoded.
        /// </summary>
        /// <value>If <c>true</c>, value should be HTML-encoded. Default is <c>true</c>.</value>
        public virtual bool HtmlEncode
        {
            get { return _htmlEncode; }
            set { _htmlEncode = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the "HiddenInput" display template should return
        /// <c>string.Empty</c> (not the expression value) and whether the "HiddenInput" editor template should not
        /// also return the expression value (together with the hidden &lt;input&gt; element).
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, also causes the default <see cref="object"/> display and editor templates to return HTML
        /// lacking the usual per-property &lt;div&gt; wrapper around the associated property. Thus the default
        /// <see cref="object"/> display template effectively skips the property and the default <see cref="object"/>
        /// editor template returns only the hidden &lt;input&gt; element for the property.
        /// </remarks>
        public virtual bool HideSurroundingHtml { get; set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ModelType"/> is a collection <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if the <see cref="ModelType"/> is not <see cref="string"/> and is assignable to
        /// <see cref="System.Collections.IEnumerable"/>; <c>false</c> otherwise.
        /// </remarks>
        public virtual bool IsCollectionType
        {
            get { return TypeHelper.IsCollectionType(ModelType); }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ModelType"/> is a complex type.
        /// </summary>
        /// <remarks>
        /// <c>false</c> if the <see cref="ModelType"/> has a direct conversion to <see cref="string"/>; <c>true</c>
        /// otherwise.
        /// </remarks>
        public virtual bool IsComplexType
        {
            get { return !TypeHelper.HasStringConverter(ModelType); }
        }

        public bool IsNullableValueType
        {
            get { return ModelType.IsNullableValueType(); }
        }

        public virtual bool IsReadOnly { get; set; }

        public virtual bool IsRequired
        {
            get { return _isRequired; }
            set { _isRequired = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating where the current metadata should be ordered relative to other properties
        /// in its containing type.
        /// </summary>
        /// <remarks>
        /// <para>For example this property is used to order items in <see cref="Properties"/>.</para>
        /// <para>The default order is <c>10000</c>.</para>
        /// </remarks>
        /// <value>The order value of the current metadata.</value>
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

        /// <summary>
        /// Gets the collection of <see cref="ModelMetadata"/> instances for the model's properties.
        /// </summary>
        public virtual ModelPropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    var properties = Provider.GetMetadataForProperties(Model, RealModelType);
                    _properties = new ModelPropertyCollection(properties.OrderBy(m => m.Order));
                }

                return _properties;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IPropertyBindingPredicateProvider"/>, which can determine which properties
        /// should be model bound.
        /// </summary>
        public virtual IPropertyBindingPredicateProvider PropertyBindingPredicateProvider { get; set; }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        protected IModelMetadataProvider Provider { get; set; }

        /// <returns>
        /// Gets runtime <see cref="Type"/> of <see cref="Model"/> if <see cref="Model"/> is non-<c>null</c> and
        /// <see cref="ModelType"/> is not <see cref="Nullable{T}"/>; <see cref="ModelType"/> otherwise.
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

        /// <summary>
        /// Gets or sets a hint that suggests what template to use for this model. Overrides <see cref="DataTypeName"/>
        /// in that context but, unlike <see cref="DataTypeName"/>, this value is not used elsewhere.
        /// </summary>
        /// <value><c>null</c> unless set manually or through additional metadata e.g. attributes.</value>
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

        public string GetDisplayName()
        {
            return DisplayName ?? PropertyName ?? ModelType.Name;
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
