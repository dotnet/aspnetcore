// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding collection values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the collection.</typeparam>
    public class CollectionModelBinder<TElement> : ICollectionModelBinder
    {
        private static readonly IValueProvider EmptyValueProvider = new CompositeValueProvider();
        private Func<object> _modelCreator;

        /// <summary>
        /// <para>This constructor is obsolete and will be removed in a future version. The recommended alternative
        /// is the overload that also takes an <see cref="ILoggerFactory"/>.</para>
        /// <para>Creates a new <see cref="CollectionModelBinder{TElement}"/>.</para>
        /// </summary>
        /// <param name="elementBinder">The <see cref="IModelBinder"/> for binding elements.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative"
            + " is the overload that also takes an " + nameof(ILoggerFactory) + ".")]
        public CollectionModelBinder(IModelBinder elementBinder)
            : this(elementBinder, NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CollectionModelBinder{TElement}"/>.
        /// </summary>
        /// <param name="elementBinder">The <see cref="IModelBinder"/> for binding elements.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public CollectionModelBinder(IModelBinder elementBinder, ILoggerFactory loggerFactory)
            : this(elementBinder, loggerFactory, allowValidatingTopLevelNodes: true)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CollectionModelBinder{TElement}"/>.
        /// </summary>
        /// <param name="elementBinder">The <see cref="IModelBinder"/> for binding elements.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="allowValidatingTopLevelNodes">
        /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
        /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
        /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
        /// </param>
        public CollectionModelBinder(
            IModelBinder elementBinder,
            ILoggerFactory loggerFactory,
            bool allowValidatingTopLevelNodes)
        {
            if (elementBinder == null)
            {
                throw new ArgumentNullException(nameof(elementBinder));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            ElementBinder = elementBinder;
            Logger = loggerFactory.CreateLogger(GetType());
            AllowValidatingTopLevelNodes = allowValidatingTopLevelNodes;
        }

        // Internal for testing.
        internal bool AllowValidatingTopLevelNodes { get; }

        /// <summary>
        /// Gets the <see cref="IModelBinder"/> instances for binding collection elements.
        /// </summary>
        protected IModelBinder ElementBinder { get; }

        /// <summary>
        /// The <see cref="ILogger"/> used for logging in this binder.
        /// </summary>
        protected ILogger Logger { get; }

        /// <inheritdoc />
        public virtual async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            Logger.AttemptingToBindModel(bindingContext);

            var model = bindingContext.Model;
            if (!bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
            {
                Logger.FoundNoValueInRequest(bindingContext);

                // If we failed to find data for a top-level model, then generate a
                // default 'empty' model (or use existing Model) and return it.
                if (bindingContext.IsTopLevelObject)
                {
                    if (model == null)
                    {
                        model = CreateEmptyCollection(bindingContext.ModelType);
                    }

                    if (AllowValidatingTopLevelNodes)
                    {
                        AddErrorIfBindingRequired(bindingContext);
                    }

                    bindingContext.Result = ModelBindingResult.Success(model);
                }

                Logger.DoneAttemptingToBindModel(bindingContext);
                return;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            CollectionResult result;
            if (valueProviderResult == ValueProviderResult.None)
            {
                Logger.NoNonIndexBasedFormatFoundForCollection(bindingContext);
                result = await BindComplexCollection(bindingContext);
            }
            else
            {
                result = await BindSimpleCollection(bindingContext, valueProviderResult);
            }

            var boundCollection = result.Model;
            if (model == null)
            {
                model = ConvertToCollectionType(bindingContext.ModelType, boundCollection);
            }
            else
            {
                // Special case for TryUpdateModelAsync(collection, ...) scenarios. Model is null in all other cases.
                CopyToModel(model, boundCollection);
            }

            Debug.Assert(model != null);
            if (result.ValidationStrategy != null)
            {
                bindingContext.ValidationState.Add(model, new ValidationStateEntry()
                {
                    Strategy = result.ValidationStrategy,
                });
            }

            if (valueProviderResult != ValueProviderResult.None)
            {
                // If we did simple binding, then modelstate should be updated to reflect what we bound for ModelName.
                // If we did complex binding, there will already be an entry for each index.
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    valueProviderResult);
            }

            bindingContext.Result = ModelBindingResult.Success(model);
            Logger.DoneAttemptingToBindModel(bindingContext);
        }

        /// <inheritdoc />
        public virtual bool CanCreateInstance(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(List<TElement>)))
            {
                // Simple case such as ICollection<TElement>, IEnumerable<TElement> and IList<TElement>.
                return true;
            }

            return targetType.GetTypeInfo().IsClass &&
                !targetType.GetTypeInfo().IsAbstract &&
                typeof(ICollection<TElement>).IsAssignableFrom(targetType);
        }

        /// <summary>
        /// Add a <see cref="ModelError" /> to <see cref="ModelBindingContext.ModelState" /> if
        /// <see cref="ModelMetadata.IsBindingRequired" />.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <remarks>
        /// For back-compatibility reasons, <see cref="ModelBindingContext.Result" /> must have
        /// <see cref="ModelBindingResult.IsModelSet" /> equal to <see langword="true" /> when a
        /// top-level model is not bound. Therefore, ParameterBinder can not detect a
        /// <see cref="ModelMetadata.IsBindingRequired" /> failure for collections. Add the error here.
        /// </remarks>
        protected void AddErrorIfBindingRequired(ModelBindingContext bindingContext)
        {
            var modelMetadata = bindingContext.ModelMetadata;
            if (modelMetadata.IsBindingRequired)
            {
                var messageProvider = modelMetadata.ModelBindingMessageProvider;
                var message = messageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
            }
        }

        /// <summary>
        /// Create an <see cref="object"/> assignable to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <returns>An <see cref="object"/> assignable to <paramref name="targetType"/>.</returns>
        /// <remarks>Called when creating a default 'empty' model for a top level bind.</remarks>
        protected virtual object CreateEmptyCollection(Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(List<TElement>)))
            {
                // Simple case such as ICollection<TElement>, IEnumerable<TElement> and IList<TElement>.
                return new List<TElement>();
            }

            return CreateInstance(targetType);
        }

        /// <summary>
        /// Create an instance of <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <returns>An instance of <paramref name="targetType"/>.</returns>
        protected object CreateInstance(Type targetType)
        {
            if (_modelCreator == null)
            {
                _modelCreator = Expression
                    .Lambda<Func<object>>(Expression.New(targetType))
                    .Compile();
            }

            return _modelCreator();

        }

        // Used when the ValueProvider contains the collection to be bound as a single element, e.g. the raw value
        // is [ "1", "2" ] and needs to be converted to an int[].
        // Internal for testing.
        internal async Task<CollectionResult> BindSimpleCollection(
            ModelBindingContext bindingContext,
            ValueProviderResult values)
        {
            var boundCollection = new List<TElement>();

            var elementMetadata = bindingContext.ModelMetadata.ElementMetadata;

            foreach (var value in values)
            {
                bindingContext.ValueProvider = new CompositeValueProvider
                {
                    // our temporary provider goes at the front of the list
                    new ElementalValueProvider(bindingContext.ModelName, value, values.Culture),
                    bindingContext.ValueProvider
                };

                // Enter new scope to change ModelMetadata and isolate element binding operations.
                using (bindingContext.EnterNestedScope(
                    elementMetadata,
                    fieldName: bindingContext.FieldName,
                    modelName: bindingContext.ModelName,
                    model: null))
                {
                    await ElementBinder.BindModelAsync(bindingContext);

                    if (bindingContext.Result.IsModelSet)
                    {
                        var boundValue = bindingContext.Result.Model;
                        boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
                    }
                }
            }

            return new CollectionResult
            {
                Model = boundCollection
            };
        }

        // Used when the ValueProvider contains the collection to be bound as multiple elements, e.g. foo[0], foo[1].
        private Task<CollectionResult> BindComplexCollection(ModelBindingContext bindingContext)
        {
            Logger.AttemptingToBindCollectionUsingIndices(bindingContext);

            var indexPropertyName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "index");

            // Remove any value provider that may not use indexPropertyName as-is. Don't match e.g. Model[index].
            var valueProvider = bindingContext.ValueProvider;
            if (valueProvider is IKeyRewriterValueProvider keyRewriterValueProvider)
            {
                valueProvider = keyRewriterValueProvider.Filter() ?? EmptyValueProvider;
            }

            var valueProviderResultIndex = valueProvider.GetValue(indexPropertyName);
            var indexNames = GetIndexNamesFromValueProviderResult(valueProviderResultIndex);

            return BindComplexCollectionFromIndexes(bindingContext, indexNames);
        }

        // Internal for testing.
        internal async Task<CollectionResult> BindComplexCollectionFromIndexes(
            ModelBindingContext bindingContext,
            IEnumerable<string> indexNames)
        {
            bool indexNamesIsFinite;
            if (indexNames != null)
            {
                indexNamesIsFinite = true;
            }
            else
            {
                indexNamesIsFinite = false;
                indexNames = Enumerable.Range(0, int.MaxValue)
                                       .Select(i => i.ToString(CultureInfo.InvariantCulture));
            }

            var elementMetadata = bindingContext.ModelMetadata.ElementMetadata;

            var boundCollection = new List<TElement>();

            foreach (var indexName in indexNames)
            {
                var fullChildName = ModelNames.CreateIndexModelName(bindingContext.ModelName, indexName);

                ModelBindingResult? result;
                using (bindingContext.EnterNestedScope(
                    elementMetadata,
                    fieldName: indexName,
                    modelName: fullChildName,
                    model: null))
                {
                    await ElementBinder.BindModelAsync(bindingContext);
                    result = bindingContext.Result;
                }

                var didBind = false;
                object boundValue = null;
                if (result != null && result.Value.IsModelSet)
                {
                    didBind = true;
                    boundValue = result.Value.Model;
                }

                // infinite size collection stops on first bind failure
                if (!didBind && !indexNamesIsFinite)
                {
                    break;
                }

                boundCollection.Add(ModelBindingHelper.CastOrDefault<TElement>(boundValue));
            }

            return new CollectionResult
            {
                Model = boundCollection,

                // If we're working with a fixed set of indexes then this is the format like:
                //
                //  ?parameter.index=zero&parameter.index=one&parameter.index=two&parameter[zero]=0&parameter[one]=1&parameter[two]=2...
                //
                // We need to provide this data to the validation system so it can 'replay' the keys.
                // But we can't just set ValidationState here, because it needs the 'real' model.
                ValidationStrategy = indexNamesIsFinite ?
                    new ExplicitIndexCollectionValidationStrategy(indexNames) :
                    null,
            };
        }

        // Internal for testing.
        internal class CollectionResult
        {
            public IEnumerable<TElement> Model { get; set; }

            public IValidationStrategy ValidationStrategy { get; set; }
        }

        /// <summary>
        /// Gets an <see cref="object"/> assignable to <paramref name="targetType"/> that contains members from
        /// <paramref name="collection"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <param name="collection">
        /// Collection of values retrieved from value providers. <see langword="null"/> if nothing was bound.
        /// </param>
        /// <returns>
        /// An <see cref="object"/> assignable to <paramref name="targetType"/>. <see langword="null"/> if nothing
        /// was bound.
        /// </returns>
        /// <remarks>
        /// Extensibility point that allows the bound collection to be manipulated or transformed before being
        /// returned from the binder.
        /// </remarks>
        protected virtual object ConvertToCollectionType(Type targetType, IEnumerable<TElement> collection)
        {
            if (collection == null)
            {
                return null;
            }

            if (targetType.IsAssignableFrom(typeof(List<TElement>)))
            {
                // Depends on fact BindSimpleCollection() and BindComplexCollection() always return a List<TElement>
                // instance or null.
                return collection;
            }

            var newCollection = CreateInstance(targetType);
            CopyToModel(newCollection, collection);

            return newCollection;
        }

        /// <summary>
        /// Adds values from <paramref name="sourceCollection"/> to given <paramref name="target"/>.
        /// </summary>
        /// <param name="target"><see cref="object"/> into which values are copied.</param>
        /// <param name="sourceCollection">
        /// Collection of values retrieved from value providers. <see langword="null"/> if nothing was bound.
        /// </param>
        protected virtual void CopyToModel(object target, IEnumerable<TElement> sourceCollection)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var targetCollection = target as ICollection<TElement>;
            Debug.Assert(targetCollection != null, "This binder is instantiated only for ICollection<T> model types.");

            if (sourceCollection != null && targetCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var element in sourceCollection)
                {
                    targetCollection.Add(element);
                }
            }
        }

        private static IEnumerable<string> GetIndexNamesFromValueProviderResult(ValueProviderResult valueProviderResult)
        {
            IEnumerable<string> indexNames = null;
            if (valueProviderResult != null)
            {
                var indexes = (string[])valueProviderResult;
                if (indexes != null && indexes.Length > 0)
                {
                    indexNames = indexes;
                }
            }

            return indexNames;
        }
    }
}
