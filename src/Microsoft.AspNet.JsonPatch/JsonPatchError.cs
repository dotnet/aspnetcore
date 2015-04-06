// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch.Operations;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.JsonPatch
{
    /// <summary>
    /// Captures error message and the related entity and the operation that caused it.
    /// </summary>
    public class JsonPatchError<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonPatchError{T}"/>.
        /// </summary>
        /// <param name="affectedObject">The object that is affected by the error.</param>
        /// <param name="operation">The <see cref="Operation{T}"/> that caused the error.</param>
        /// <param name="errorMessage">The error message.</param>
        public JsonPatchError(
            [NotNull] T affectedObject,
            [NotNull] Operation<T> operation,
            [NotNull] string errorMessage)
        {
            AffectedObject = affectedObject;
            Operation = operation;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets the object that is affected by the error.
        /// </summary>
        public T AffectedObject { get; }

        /// <summary>
        /// Gets the <see cref="Operation{T}"/> that caused the error.
        /// </summary>
        public Operation<T> Operation { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }
    }
}