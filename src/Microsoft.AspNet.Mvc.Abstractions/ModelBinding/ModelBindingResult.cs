// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Contains the result of model binding.
    /// </summary>
    public struct ModelBindingResult : IEquatable<ModelBindingResult>
    {
        /// <summary>
        /// A <see cref="ModelBinding"/> representing the lack of a result. The model binding
        /// system will continue to execute other model binders.
        /// </summary>
        public static readonly ModelBindingResult NoResult = new ModelBindingResult();

        /// <summary>
        /// Returns a completed <see cref="Task{ModelBindingResult}"/> representing the lack of a result. The model
        /// binding system will continue to execute other model binders.
        /// </summary>
        public static readonly Task<ModelBindingResult> NoResultAsync = Task.FromResult(NoResult);

        /// <summary>
        /// Creates a <see cref="ModelBindingResult"/> representing a failed model binding operation.
        /// </summary>
        /// <param name="key">The key of the current model binding operation.</param>
        /// <returns>A <see cref="ModelBindingResult"/> representing a failed model binding operation.</returns>
        public static ModelBindingResult Failed([NotNull] string key)
        {
            return new ModelBindingResult(key, model: null, isModelSet: false);
        }

        /// <summary>
        /// Creates a completed <see cref="Task{ModelBindingResult}"/> representing a failed model binding operation.
        /// </summary>
        /// <param name="key">The key of the current model binding operation.</param>
        /// <returns>A completed <see cref="Task{ModelBindingResult}"/> representing a failed model binding operation.</returns>
        public static Task<ModelBindingResult> FailedAsync([NotNull] string key)
        {
            return Task.FromResult(Failed(key));
        }

        /// <summary>
        /// Creates a <see cref="ModelBindingResult"/> representing a successful model binding operation.
        /// </summary>
        /// <param name="key">The key of the current model binding operation.</param>
        /// <param name="model">The model value. May be <c>null.</c></param>
        /// <returns>A <see cref="ModelBindingResult"/> representing a successful model bind.</returns>
        public static ModelBindingResult Success(
            [NotNull] string key,
            object model)
        {
            return new ModelBindingResult(key, model, isModelSet: true);
        }

        /// <summary>
        /// Creates a completed <see cref="Task{ModelBindingResult}"/> representing a successful model binding
        /// operation.
        /// </summary>
        /// <param name="key">The key of the current model binding operation.</param>
        /// <param name="model">The model value. May be <c>null.</c></param>
        /// <returns>A completed <see cref="Task{ModelBindingResult}"/> representing a successful model bind.</returns>
        public static Task<ModelBindingResult> SuccessAsync(
            [NotNull] string key,
            object model)
        {
            return Task.FromResult(Success(key, model));
        }

        private ModelBindingResult(string key, object model, bool isModelSet)
        {
            Key = key;
            Model = model;
            IsModelSet = isModelSet;
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

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as ModelBindingResult?;
            if (other == null)
            {
                return false;
            }
            else
            {
                return Equals(other.Value);
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Key, StringComparer.OrdinalIgnoreCase);
            hashCodeCombiner.Add(IsModelSet);
            hashCodeCombiner.Add(Model);

            return hashCodeCombiner.CombinedHash;
        }

        /// <inheritdoc />
        public bool Equals(ModelBindingResult other)
        {
            return
                string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) &&
                IsModelSet == other.IsModelSet &&
                object.Equals(Model, other.Model);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Key == null)
            {
                return "No Result";
            }
            else if (IsModelSet)
            {
                return $"Success {Key} -> '{Model}'";
            }
            else
            {
                return $"Failed {Key}";
            }
        }

        /// <summary>
        /// Compares <see cref="ModelBindingResult"/> objects for equality.
        /// </summary>
        /// <param name="x">A <see cref="ModelBindingResult"/>.</param>
        /// <param name="y">A <see cref="ModelBindingResult"/>.</param>
        /// <returns><c>true</c> if the objects are equal, otherwise <c>false</c>.</returns>
        public static bool operator ==(ModelBindingResult x, ModelBindingResult y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Compares <see cref="ModelBindingResult"/> objects for inequality.
        /// </summary>
        /// <param name="x">A <see cref="ModelBindingResult"/>.</param>
        /// <param name="y">A <see cref="ModelBindingResult"/>.</param>
        /// <returns><c>true</c> if the objects are not equal, otherwise <c>false</c>.</returns>
        public static bool operator !=(ModelBindingResult x, ModelBindingResult y)
        {
            return !x.Equals(y);
        }
    }
}
