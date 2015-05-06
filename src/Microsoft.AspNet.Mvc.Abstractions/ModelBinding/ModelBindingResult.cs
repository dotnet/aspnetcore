// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Contains the result of model binding.
    /// </summary>
    public class ModelBindingResult
    {
        /// <summary>
        /// Creates a new <see cref="ModelBindingResult"/>.
        /// </summary>
        /// <param name="model">The model which was created by the <see cref="IModelBinder"/>.</param>
        /// <param name="key">The key using which was used to attempt binding the model.</param>
        /// <param name="isModelSet">A value that represents if the model has been set by the
        /// <see cref="IModelBinder"/>.</param>
        public ModelBindingResult(object model, string key, bool isModelSet)
            : this (model, key, isModelSet, validationNode: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ModelBindingResult"/>.
        /// </summary>
        /// <param name="model">The model which was created by the <see cref="IModelBinder"/>.</param>
        /// <param name="key">The key using which was used to attempt binding the model.</param>
        /// <param name="isModelSet">A value that represents if the model has been set by the
        /// <see cref="IModelBinder"/>.</param>
        /// <param name="validationNode">A <see cref="ModelValidationNode"/> which captures the validation information.
        /// </param>
        public ModelBindingResult(object model, string key, bool isModelSet, ModelValidationNode validationNode)
        {
            Model = model;
            Key = key;
            IsModelSet = isModelSet;
            ValidationNode = validationNode;
        }

        /// <summary>
        /// Gets the model associated with this context.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// <para>
        /// Gets the model name which was used to bind the model.
        /// </para>
        /// <para>
        /// This property can be used during validation to add model state for a bound model.
        /// </para>
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// <para>
        /// Gets a value indicating whether or not the <see cref="Model"/> value has been set.
        /// </para>
        /// <para>
        /// This property can be used to distinguish between a model binder which does not find a value and
        /// the case where a model binder sets the <c>null</c> value.
        /// </para>
        /// </summary>
        public bool IsModelSet { get; }

        /// <summary>
        /// A <see cref="ModelValidationNode"/> associated with the current <see cref="ModelBindingResult"/>.
        /// </summary>
        public ModelValidationNode ValidationNode { get; }
    }
}
