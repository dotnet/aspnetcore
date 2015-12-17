// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Defines an interface for model binders.
    /// </summary>
    public interface IModelBinder
    {
        /// <summary>
        /// Attempts to bind a model.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// <para>
        /// A <see cref="Task"/> which on completion returns a <see cref="ModelBindingResult"/> which
        /// represents the result of the model binding process.
        /// </para>
        /// <para>
        /// If model binding was successful, the <see cref="ModelBindingResult"/> should be a value created
        /// with <see cref="ModelBindingResult.Success"/>. If model binding failed, the
        /// <see cref="ModelBindingResult"/> should be a value created with <see cref="ModelBindingResult.Failed"/>.
        /// If there was no data, or this model binder cannot handle the operation, the
        /// <see cref="ModelBindingResult"/> should be <see cref="ModelBindingResult.NoResult"/>.
        /// </para>
        /// </returns>
        Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext);
    }
}
