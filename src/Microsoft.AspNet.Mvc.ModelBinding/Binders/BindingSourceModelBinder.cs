// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which provides data from a specific <see cref="ModelBinding.BindingSource"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="BindingSourceModelBinder"/> is an <see cref="IModelBinder"/> base-implementation which
    /// can provide data for all parameters and model properties which specify the corresponding
    /// <see cref="ModelBinding.BindingSource"/>.
    /// </para>
    /// <para>
    /// <see cref="BindingSourceModelBinder"/> is greedy, meaning that a given instance expects to handle all
    /// parameters and properties annotated with the corresponding <see cref="ModelBinding.BindingSource"/> and
    /// will short-circuit the model binding process to prevent other binders from running.
    /// <see cref="ModelBinding.BindingSource.IsGreedy"/> of <see cref="BindingSource"/> must be set to <c>true.</c>
    /// </para>
    /// </remarks>
    public abstract class BindingSourceModelBinder : IModelBinder
    {
        /// <summary>
        /// Creates a new <see cref="BindingSourceModelBinder"/>.
        /// </summary>
        /// <param name="bindingSource">
        /// The <see cref="ModelBinding.BindingSource"/>. Must be a single-source (non-composite) with
        /// <see cref="ModelBinding.BindingSource.IsGreedy"/> equal to <c>true</c>.
        /// </param>
        protected BindingSourceModelBinder([NotNull] BindingSource bindingSource)
        {
            // This class implements a pattern that's only useful for greedy model binders. If you need
            // to implement something non-greedy then don't use the base class.
            if (!bindingSource.IsGreedy)
            {
                var message = Resources.FormatBindingSource_MustBeGreedy(
                    bindingSource.DisplayName,
                    nameof(BindingSourceModelBinder));
                throw new ArgumentException(message, nameof(bindingSource));
            }

            BindingSource = bindingSource;
        }

        /// <summary>
        /// Gets the corresponding <see cref="ModelBinding.BindingSource"/>.
        /// </summary>
        protected BindingSource BindingSource { get; }

        /// <summary>
        /// Binds the model. Called when the model's supported binding-source matches <see cref="BindingSource"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> which will complete when model binding has completed.
        /// </returns>
        protected abstract Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext);

        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext context)
        {
            var bindingSourceMetadata = context.ModelMetadata.BinderMetadata as IBindingSourceMetadata;
            var allowedBindingSource = bindingSourceMetadata?.BindingSource;

            if (allowedBindingSource == null || !allowedBindingSource.CanAcceptDataFrom(BindingSource))
            {
                // Binding Sources are opt-in. This model either didn't specify one or specified something
                // incompatible so let other binders run.
                return null;
            }

            var result = await BindModelCoreAsync(context);

            var modelBindingResult = 
                result != null ? 
                    new ModelBindingResult(result.Model, result.Key, result.IsModelSet) :
                    new ModelBindingResult(null, context.ModelName, false);

            // Prevent other model binders from running because this model binder is the only handler for
            // its binding source.
            return modelBindingResult;
        }
    }
}
