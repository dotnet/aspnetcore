// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A context that contains operating information for model binding and validation.
    /// </summary>
    public class ModelBindingContext
    {
        private string _fieldName;
        private ModelMetadata _modelMetadata;
        private string _modelName;
        private ModelStateDictionary _modelState;
        private OperationBindingContext _operationBindingContext;
        private IValueProvider _valueProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBindingContext"/> class.
        /// </summary>
        public ModelBindingContext()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ModelBindingContext"/> for top-level model binding operation.
        /// </summary>
        /// <param name="operationBindingContext">
        /// The <see cref="OperationBindingContext"/> associated with the binding operation.
        /// </param>
        /// <param name="metadata"><see cref="ModelMetadata"/> associated with the model.</param>
        /// <param name="bindingInfo"><see cref="BindingInfo"/> associated with the model.</param>
        /// <param name="modelName">The name of the property or parameter being bound.</param>
        /// <returns>A new instance of <see cref="ModelBindingContext"/>.</returns>
        public static ModelBindingContext CreateBindingContext(
            OperationBindingContext operationBindingContext,
            ModelMetadata metadata,
            BindingInfo bindingInfo,
            string modelName)
        {
            if (operationBindingContext == null)
            {
                throw new ArgumentNullException(nameof(operationBindingContext));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            var binderModelName = bindingInfo?.BinderModelName ?? metadata.BinderModelName;
            var propertyPredicateProvider =
                bindingInfo?.PropertyBindingPredicateProvider ?? metadata.PropertyBindingPredicateProvider;

            return new ModelBindingContext()
            {
                BinderModelName = binderModelName,
                BindingSource = bindingInfo?.BindingSource ?? metadata.BindingSource,
                BinderType = bindingInfo?.BinderType ?? metadata.BinderType,
                PropertyFilter = propertyPredicateProvider?.PropertyFilter,

                // We only support fallback to empty prefix in cases where the model name is inferred from
                // the parameter or property being bound.
                FallbackToEmptyPrefix = binderModelName == null,

                // Because this is the top-level context, FieldName and ModelName should be the same.
                FieldName = binderModelName ?? modelName,
                ModelName = binderModelName ?? modelName,

                IsTopLevelObject = true,
                ModelMetadata = metadata,
                ModelState = operationBindingContext.ActionContext.ModelState,
                OperationBindingContext = operationBindingContext,
                ValueProvider = operationBindingContext.ValueProvider,

                ValidationState = new ValidationStateDictionary(),
            };
        }

        public static ModelBindingContext CreateChildBindingContext(
            ModelBindingContext parent,
            ModelMetadata modelMetadata,
            string fieldName,
            string modelName,
            object model)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (modelMetadata == null)
            {
                throw new ArgumentNullException(nameof(modelMetadata));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            return new ModelBindingContext()
            {
                ModelState = parent.ModelState,
                OperationBindingContext = parent.OperationBindingContext,
                ValueProvider = parent.ValueProvider,
                ValidationState = parent.ValidationState,

                Model = model,
                ModelMetadata = modelMetadata,
                ModelName = modelName,
                FieldName = fieldName,
                BinderModelName = modelMetadata.BinderModelName,
                BinderType = modelMetadata.BinderType,
                BindingSource = modelMetadata.BindingSource,
                PropertyFilter = modelMetadata.PropertyBindingPredicateProvider?.PropertyFilter,
            };
        }

        /// <summary>
        /// Represents the <see cref="OperationBindingContext"/> associated with this context.
        /// </summary>
        public OperationBindingContext OperationBindingContext
        {
            get { return _operationBindingContext; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _operationBindingContext = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the current field being bound.
        /// </summary>
        public string FieldName
        {
            get { return _fieldName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fieldName = value;
            }
        }

        /// <summary>
        /// Gets or sets the model value for the current operation.
        /// </summary>
        /// <remarks>
        /// The <see cref="Model"/> will typically be set for a binding operation that works
        /// against a pre-existing model object to update certain properties.
        /// </remarks>
        public object Model { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the model associated with this context.
        /// </summary>
        public ModelMetadata ModelMetadata
        {
            get { return _modelMetadata; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _modelMetadata = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the model. This property is used as a key for looking up values in
        /// <see cref="IValueProvider"/> during model binding.
        /// </summary>
        public string ModelName
        {
            get { return _modelName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _modelName = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ModelStateDictionary"/> used to capture <see cref="ModelState"/> values
        /// for properties in the object graph of the model when binding.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get { return _modelState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _modelState = value;
            }
        }

        /// <summary>
        /// Gets the type of the model.
        /// </summary>
        /// <remarks>
        /// The <see cref="ModelMetadata"/> property must be set to access this property.
        /// </remarks>
        public Type ModelType => ModelMetadata?.ModelType;

        /// <summary>
        /// Gets or sets a model name which is explicitly set using an <see cref="IModelNameProvider"/>.
        /// <see cref="Model"/>.
        /// </summary>
        public string BinderModelName { get; set; }

        /// <summary>
        /// Gets or sets a value which represents the <see cref="BindingSource"/> associated with the
        /// <see cref="Model"/>.
        /// </summary>
        public BindingSource BindingSource { get; set; }

        /// <summary>
        /// Gets the <see cref="Type"/> of an <see cref="IModelBinder"/> associated with the
        /// <see cref="Model"/>.
        /// </summary>
        public Type BinderType { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the binder should use an empty prefix to look up
        /// values in <see cref="IValueProvider"/> when no values are found using the <see cref="ModelName"/> prefix.
        /// </summary>
        /// <remarks>
        /// Passed into the model binding system. Should not be <c>true</c> when <see cref="IsTopLevelObject"/> is
        /// <c>false</c>.
        /// </remarks>
        public bool FallbackToEmptyPrefix { get; set; }

        /// <summary>
        /// Gets or sets an indication that the current binder is handling the top-level object.
        /// </summary>
        /// <remarks>Passed into the model binding system.</remarks>
        public bool IsTopLevelObject { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IValueProvider"/> associated with this context.
        /// </summary>
        public IValueProvider ValueProvider
        {
            get { return _valueProvider; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets a predicate which will be evaluated for each property to determine if the property
        /// is eligible for model binding.
        /// </summary>
        public Func<ModelBindingContext, string, bool> PropertyFilter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ValidationStateDictionary"/>. Used for tracking validation state to
        /// customize validation behavior for a model object.
        /// </summary>
        public ValidationStateDictionary ValidationState { get; set; }
    }
}
