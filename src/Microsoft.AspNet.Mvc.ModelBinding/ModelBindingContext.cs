// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBindingContext
    {
        private string _modelName;
        private ModelStateDictionary _modelState;
        private Dictionary<string, ModelMetadata> _propertyMetadata;
        private ModelValidationNode _validationNode;

        public ModelBindingContext()
        {
        }

        // copies certain values that won't change between parent and child objects,
        // e.g. ValueProvider, ModelState
        public ModelBindingContext(ModelBindingContext bindingContext)
        {
            if (bindingContext != null)
            {
                ModelState = bindingContext.ModelState;
                ValueProvider = bindingContext.ValueProvider;
                MetadataProvider = bindingContext.MetadataProvider;
                ModelBinder = bindingContext.ModelBinder;
                ValidatorProviders = bindingContext.ValidatorProviders;
                HttpContext = bindingContext.HttpContext;
            }
        }

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

        public ModelMetadata ModelMetadata { get; set; }

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

        public Type ModelType
        {
            get
            {
                EnsureModelMetadata();
                return ModelMetadata.ModelType;
            }
        }

        public bool FallbackToEmptyPrefix { get; set; }

        public HttpContext HttpContext { get; set; }

        public IValueProvider ValueProvider
        {
            get;
            set;
        }

        public IModelBinder ModelBinder
        {
            get;
            set;
        }

        public IModelMetadataProvider MetadataProvider
        {
            get;
            set;
        }

        public IEnumerable<IModelValidatorProvider> ValidatorProviders
        {
            get;
            set;
        }

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
