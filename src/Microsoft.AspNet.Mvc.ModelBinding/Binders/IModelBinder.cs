// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
        /// <returns>A Task with a bool implying the success or failure of the operation.</returns>
        Task<bool> BindModelAsync(ModelBindingContext bindingContext);
    }
}
