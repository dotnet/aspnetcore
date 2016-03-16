// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A context that contains operating information for model binding and validation.
    /// </summary>
    public class DefaultModelBindingContext : ModelBindingContext
    {
        private OperationBindingContext _operationBindingContext;

        private State _state;
        private readonly Stack<State> _stack = new Stack<State>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultModelBindingContext"/> class.
        /// </summary>
        public DefaultModelBindingContext()
        {
        }

        /// <summary>
        /// Creates a new <see cref="DefaultModelBindingContext"/> for top-level model binding operation.
        /// </summary>
        /// <param name="operationBindingContext">
        /// The <see cref="OperationBindingContext"/> associated with the binding operation.
        /// </param>
        /// <param name="metadata"><see cref="ModelMetadata"/> associated with the model.</param>
        /// <param name="bindingInfo"><see cref="BindingInfo"/> associated with the model.</param>
        /// <param name="modelName">The name of the property or parameter being bound.</param>
        /// <returns>A new instance of <see cref="DefaultModelBindingContext"/>.</returns>
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

            var valueProvider = operationBindingContext.ValueProvider;
            var bindingSource = bindingInfo?.BindingSource ?? metadata.BindingSource;
            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                valueProvider = FilterValueProvider(operationBindingContext.ValueProvider, bindingSource);
            }

            return new DefaultModelBindingContext()
            {
                BinderModelName = binderModelName,
                BindingSource = bindingSource,
                PropertyFilter = propertyPredicateProvider?.PropertyFilter,

                // Because this is the top-level context, FieldName and ModelName should be the same.
                FieldName = binderModelName ?? modelName,
                ModelName = binderModelName ?? modelName,

                IsTopLevelObject = true,
                ModelMetadata = metadata,
                ModelState = operationBindingContext.ActionContext.ModelState,
                OperationBindingContext = operationBindingContext,
                ValueProvider = valueProvider,

                ValidationState = new ValidationStateDictionary(),
            };
        }

        /// <inheritdoc />
        public override NestedScope EnterNestedScope(
            ModelMetadata modelMetadata,
            string fieldName,
            string modelName,
            object model)
        {
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

            var scope = EnterNestedScope();

            // Only filter if the new BindingSource affects the value providers. Otherwise we want
            // to preserve the currrent state.
            if (modelMetadata.BindingSource != null && !modelMetadata.BindingSource.IsGreedy)
            {
                ValueProvider = FilterValueProvider(_operationBindingContext.ValueProvider, modelMetadata.BindingSource);
            }

            Model = model;
            ModelMetadata = modelMetadata;
            ModelName = modelName;
            FieldName = fieldName;
            BinderModelName = modelMetadata.BinderModelName;
            BindingSource = modelMetadata.BindingSource;
            PropertyFilter = modelMetadata.PropertyBindingPredicateProvider?.PropertyFilter;

            IsTopLevelObject = false;

            return scope;
        }

        /// <inheritdoc />
        public override NestedScope EnterNestedScope()
        {
            _stack.Push(_state);

            Result = null;

            return new NestedScope(this);
        }

        /// <inheritdoc />
        protected override void ExitNestedScope()
        {
            _state = _stack.Pop();
        }

        /// <inheritdoc />
        public override OperationBindingContext OperationBindingContext
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

        /// <inheritdoc />
        public override string FieldName
        {
            get { return _state.FieldName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _state.FieldName = value;
            }
        }

        /// <inheritdoc />
        public override object Model
        {
            get { return _state.Model; }
            set { _state.Model = value; }
        }

        /// <inheritdoc />
        public override ModelMetadata ModelMetadata
        {
            get { return _state.ModelMetadata; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _state.ModelMetadata = value;
            }
        }

        /// <inheritdoc />
        public override string ModelName
        {
            get { return _state.ModelName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _state.ModelName = value;
            }
        }

        /// <inheritdoc />
        public override ModelStateDictionary ModelState
        {
            get { return _state.ModelState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _state.ModelState = value;
            }
        }

        /// <inheritdoc />
        public override Type ModelType => ModelMetadata?.ModelType;

        /// <inheritdoc />
        public override string BinderModelName
        {
            get { return _state.BinderModelName; }
            set { _state.BinderModelName = value; }
        }

        /// <inheritdoc />
        public override BindingSource BindingSource
        {
            get { return _state.BindingSource; }
            set { _state.BindingSource = value; }
        }

        /// <inheritdoc />
        public override bool IsTopLevelObject
        {
            get { return _state.IsTopLevelObject; }
            set { _state.IsTopLevelObject = value; }
        }

        /// <inheritdoc />
        public override IValueProvider ValueProvider
        {
            get { return _state.ValueProvider; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _state.ValueProvider = value;
            }
        }

        /// <inheritdoc />
        public override Func<ModelBindingContext, string, bool> PropertyFilter
        {
            get { return _state.PropertyFilter; }
            set { _state.PropertyFilter = value; }
        }

        /// <inheritdoc />
        public override ValidationStateDictionary ValidationState
        {
            get { return _state.ValidationState; }
            set { _state.ValidationState = value; }
        }

        /// <inheritdoc />
        public override ModelBindingResult? Result
        {
            get
            {
                return _state.Result;
            }
            set
            {
                if (value.HasValue && value.Value == default(ModelBindingResult))
                {
                    throw new ArgumentException(nameof(ModelBindingResult));
                }

                _state.Result = value;
            }
        }

        private static IValueProvider FilterValueProvider(IValueProvider valueProvider, BindingSource bindingSource)
        {
            if (bindingSource == null || bindingSource.IsGreedy)
            {
                return valueProvider;
            }

            var bindingSourceValueProvider = valueProvider as IBindingSourceValueProvider;
            if (bindingSourceValueProvider == null)
            {
                return valueProvider;
            }

            return bindingSourceValueProvider.Filter(bindingSource) ?? new CompositeValueProvider();
        }

        private struct State
        {
            public string FieldName;
            public object Model;
            public ModelMetadata ModelMetadata;
            public string ModelName;

            public IValueProvider ValueProvider;
            public Func<ModelBindingContext, string, bool> PropertyFilter;
            public ValidationStateDictionary ValidationState;
            public ModelStateDictionary ModelState;

            public string BinderModelName;
            public BindingSource BindingSource;
            public bool IsTopLevelObject;

            public ModelBindingResult? Result;
        };
    }
}
