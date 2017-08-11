// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A type that wraps either an <typeparamref name="TValue"/> instance or an <see cref="ActionResult"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the result.</typeparam>
    public class ActionResult<TValue> : IConvertToActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ActionResult{TValue}"/> using the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public ActionResult(TValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Intializes a new instance of <see cref="ActionResult{TValue}"/> using the specified <see cref="ActionResult"/>.
        /// </summary>
        /// <param name="result">The <see cref="ActionResult"/>.</param>
        public ActionResult(ActionResult result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the <see cref="ActionResult"/>.
        /// </summary>
        public ActionResult Result { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TValue Value { get; }

        public static implicit operator ActionResult<TValue>(TValue value)
        {
            return new ActionResult<TValue>(value);
        }

        public static implicit operator ActionResult<TValue>(ActionResult result)
        {
            return new ActionResult<TValue>(result);
        }

        IActionResult IConvertToActionResult.Convert()
        {
            return Result ?? new ObjectResult(Value)
            {
                DeclaredType = typeof(TValue),
            };
        }
    }
}
