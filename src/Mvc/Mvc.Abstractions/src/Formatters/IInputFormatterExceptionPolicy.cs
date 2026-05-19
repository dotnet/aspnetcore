// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A policy which <see cref="IInputFormatter"/>s can implement to indicate if they want the body model binder
/// to handle all exceptions. By default, all default <see cref="IInputFormatter"/>s implement this interface and
/// have a default value of <see cref="InputFormatterExceptionPolicy.MalformedInputExceptions"/>.
/// </summary>
public interface IInputFormatterExceptionPolicy
{
    /// <summary>
    /// Gets the flag to indicate if the body model binder should handle all exceptions. If an exception is handled,
    /// the body model binder converts the exception into model state errors, else the exception is allowed to propagate.
    /// </summary>
    InputFormatterExceptionPolicy ExceptionPolicy { get; }
}
