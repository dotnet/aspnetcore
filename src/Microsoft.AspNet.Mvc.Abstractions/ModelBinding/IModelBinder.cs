// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Interface for model binding.
    /// </summary>
    public interface IModelBinder
    {
        /// <summary>
        /// Async function to bind to a particular model.
        /// </summary>
        /// <param name="bindingContext">The binding context which has the object to be bound.</param>
        /// <returns>A Task which on completion returns a <see cref="ModelBindingResult"/> which represents the result
        /// of the model binding process.
        /// </returns>
        /// <remarks>
        /// A <c>null</c> return value means that this model binder was not able to handle the request.
        /// Returning <c>null</c> ensures that subsequent model binders are run. If a non <c>null</c> value indicates 
        /// that the model binder was able to handle the request.
        /// </remarks>
        Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext);
    }
}
