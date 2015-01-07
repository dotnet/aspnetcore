// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A context that contains operating information for model binding and validation.
    /// </summary>
    public class ModelBindingContext
    {
        private static readonly Func<ModelBindingContext, string, bool>
            _defaultPropertyFilter = (context, propertyName) => true;

        private string _modelName;
        private ModelStateDictionary _modelState;
        private Dictionary<string, ModelMetadata> _propertyMetadata;
        private ModelValidationNode _validationNode;
        private Func<ModelBindingContext, string, bool> _propertyFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBindingContext"/> class.
        /// </summary>
        public ModelBindingContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBindingContext"/> class using the
        /// <paramref name="bindingContext" />.
        /// </summary>
        /// <param name="bindingContext">Existing <see cref="ModelBindingContext"/>.</param>
        /// <param name="modelName">Model name of associated with the new <see cref="ModelBindingContext"/>.</param>
        /// <param name="modelMetadata">Model metadata of associated with the new <see cref="ModelBindingContext"/>.
        /// </param>
        /// <remarks>
        /// This constructor copies certain values that won't change between parent and child objects,
        /// e.g. ValueProvider, ModelState
        /// </remarks>
        public ModelBindingContext([NotNull] ModelBindingContext bindingContext,
                                   [NotNull] string modelName,
                                   [NotNull] ModelMetadata modelMetadata)
        {
            ModelName = modelName;
            ModelMetadata = modelMetadata;
            ModelState = bindingContext.ModelState;
            ValueProvider = bindingContext.ValueProvider;
            OperationBindingContext = bindingContext.OperationBindingContext;
        }

        /// <summary>
        /// Represents the <see cref="OperationBindingContext"/> associated with this context.
        /// </summary>
        public OperationBindingContext OperationBindingContext { get; set; }

        /// <summary>
        /// Gets or sets the model associated with this context.
        /// </summary>
        /// <remarks>
        /// The <see cref="ModelMetadata"/> property must be set to access this property.
        /// </remarks>
        public object Model
        {
            get
            {
                EnsureModelMetadata();
                return ModelMetadata.Model;
            }
            set
            {
                EnsureModelMetadata();
                ModelMetadata.Model = value;
            }
        }

        /// <summary>
        /// Gets or sets the metadata for the model associated with this context.
        /// </summary>
        public ModelMetadata ModelMetadata { get; set; }

        /// <summary>
        /// Gets or sets the name of the model. This property is used as a key for looking up values in
        /// <see cref="IValueProvider"/> during model binding.
        /// </summary>
        public string ModelName
        {
            get
            {
                if (_modelName == null)
                {
                    _modelName = string.Empty;
                }
                return _modelName;
            }
            set { _modelName = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ModelStateDictionary"/> used to capture <see cref="ModelState"/> values
        /// for properties in the object graph of the model when binding.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get
            {
                if (_modelState == null)
                {
                    _modelState = new ModelStateDictionary();
                }
                return _modelState;
            }
            set { _modelState = value; }
        }

        /// <summary>
        /// Gets the type of the model.
        /// </summary>
        /// <remarks>
        /// The <see cref="ModelMetadata"/> property must be set to access this property.
        /// </remarks>
        public Type ModelType
        {
            get
            {
                EnsureModelMetadata();
                return ModelMetadata.ModelType;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the binder should use an empty prefix to look up
        /// values in <see cref="IValueProvider"/> when no values are found using the
        /// <see cref="ModelName"/> prefix.
        /// </summary>
        public bool FallbackToEmptyPrefix { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IValueProvider"/> associated with this context.
        /// </summary>
        public IValueProvider ValueProvider { get; set; }

        /// <summary>
        /// Gets a dictionary of property name to <see cref="ModelMetadata"/> instances for
        /// <see cref="ModelMetadata.Properties"/>
        /// </summary>
        public IDictionary<string, ModelMetadata> PropertyMetadata
        {
            get
            {
                if (_propertyMetadata == null)
                {
                    _propertyMetadata = ModelMetadata.Properties
                                                     .ToDictionary(m => m.PropertyName,
                                                                   StringComparer.OrdinalIgnoreCase);
                }

                return _propertyMetadata;
            }
        }

        public Func<ModelBindingContext, string, bool> PropertyFilter
        {
            get
            {
                if (_propertyFilter == null)
                {
                    _propertyFilter = _defaultPropertyFilter;
                }
                return _propertyFilter;
            }
            set { _propertyFilter = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ModelValidationNode"/> instance used as a container for
        /// validation information.
        /// </summary>
        public ModelValidationNode ValidationNode
        {
            get
            {
                if (_validationNode == null)
                {
                    _validationNode = new ModelValidationNode(ModelMetadata, ModelName);
                }
                return _validationNode;
            }
            set { _validationNode = value; }
        }

        private void EnsureModelMetadata()
        {
            if (ModelMetadata == null)
            {
                throw new InvalidOperationException(Resources.ModelBindingContext_ModelMetadataMustBeSet);
            }
        }
    }
}
