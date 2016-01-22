// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Result of a <see cref="IInputFormatter.ReadAsync"/> operation.
    /// </summary>
    public class InputFormatterResult
    {
        private static readonly InputFormatterResult _failure = new InputFormatterResult();
        private static readonly Task<InputFormatterResult> _failureAsync = Task.FromResult(_failure);

        private InputFormatterResult()
        {
            HasError = true;
        }

        private InputFormatterResult(object model)
        {
            Model = model;
        }

        /// <summary>
        /// Gets an indication whether the <see cref="IInputFormatter.ReadAsync"/> operation had an error.
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Gets the deserialized <see cref="object"/>.
        /// </summary>
        /// <value>
        /// <c>null</c> if <see cref="HasError"/> is <c>true</c>.
        /// </value>
        public object Model { get; }

        /// <summary>
        /// Returns an <see cref="InputFormatterResult"/> indicating the <see cref="IInputFormatter.ReadAsync"/>
        /// operation failed.
        /// </summary>
        /// <returns>
        /// An <see cref="InputFormatterResult"/> indicating the <see cref="IInputFormatter.ReadAsync"/>
        /// operation failed i.e. with <see cref="HasError"/> <c>true</c>.
        /// </returns>
        public static InputFormatterResult Failure()
        {
            return _failure;
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that on completion provides an <see cref="InputFormatterResult"/> indicating
        /// the <see cref="IInputFormatter.ReadAsync"/> operation failed.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides an <see cref="InputFormatterResult"/> indicating the
        /// <see cref="IInputFormatter.ReadAsync"/> operation failed i.e. with <see cref="HasError"/> <c>true</c>.
        /// </returns>
        public static Task<InputFormatterResult> FailureAsync()
        {
            return _failureAsync;
        }

        /// <summary>
        /// Returns an <see cref="InputFormatterResult"/> indicating the <see cref="IInputFormatter.ReadAsync"/>
        /// operation was successful.
        /// </summary>
        /// <param name="model">The deserialized <see cref="object"/>.</param>
        /// <returns>
        /// An <see cref="InputFormatterResult"/> indicating the <see cref="IInputFormatter.ReadAsync"/>
        /// operation succeeded i.e. with <see cref="HasError"/> <c>false</c>.
        /// </returns>
        public static InputFormatterResult Success(object model)
        {
            return new InputFormatterResult(model);
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that on completion provides an <see cref="InputFormatterResult"/> indicating
        /// the <see cref="IInputFormatter.ReadAsync"/> operation was successful.
        /// </summary>
        /// <param name="model">The deserialized <see cref="object"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides an <see cref="InputFormatterResult"/> indicating the
        /// <see cref="IInputFormatter.ReadAsync"/> operation succeeded i.e. with <see cref="HasError"/> <c>false</c>.
        /// </returns>
        public static Task<InputFormatterResult> SuccessAsync(object model)
        {
            return Task.FromResult(Success(model));
        }
    }
}
