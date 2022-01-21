// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Reads an object from the request body.
/// </summary>
public interface IInputFormatter
{
    /// <summary>
    /// Determines whether this <see cref="IInputFormatter"/> can deserialize an object of the
    /// <paramref name="context"/>'s <see cref="InputFormatterContext.ModelType"/>.
    /// </summary>
    /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
    /// <returns>
    /// <c>true</c> if this <see cref="IInputFormatter"/> can deserialize an object of the
    /// <paramref name="context"/>'s <see cref="InputFormatterContext.ModelType"/>. <c>false</c> otherwise.
    /// </returns>
    bool CanRead(InputFormatterContext context);

    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
    /// <returns>A <see cref="Task"/> that on completion deserializes the request body.</returns>
    Task<InputFormatterResult> ReadAsync(InputFormatterContext context);
}
