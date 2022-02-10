// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Microsoft.AspNetCore.JsonPatch;

/// <summary>
/// Captures error message and the related entity and the operation that caused it.
/// </summary>
public class JsonPatchError
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPatchError"/>.
    /// </summary>
    /// <param name="affectedObject">The object that is affected by the error.</param>
    /// <param name="operation">The <see cref="Operation"/> that caused the error.</param>
    /// <param name="errorMessage">The error message.</param>
    public JsonPatchError(
        object affectedObject,
        Operation operation,
        string errorMessage)
    {
        if (errorMessage == null)
        {
            throw new ArgumentNullException(nameof(errorMessage));
        }

        AffectedObject = affectedObject;
        Operation = operation;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the object that is affected by the error.
    /// </summary>
    public object AffectedObject { get; }

    /// <summary>
    /// Gets the <see cref="Operation"/> that caused the error.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; }
}
