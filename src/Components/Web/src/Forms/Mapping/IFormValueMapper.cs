// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

/// <summary>
/// Maps form data values to a model.
/// </summary>
public interface IFormValueMapper
{
    /// <summary>
    /// Determines whether the specified value type can be mapped.
    /// </summary>
    /// <param name="valueType">The <see cref="Type"/> for the value to map.</param>
    /// <param name="formName">The form name to map data from or null to only validate the type can be mapped.</param>
    /// <returns><c>true</c> if the value type can be mapped; otherwise, <c>false</c>.</returns>
    bool CanMap(Type valueType, string? formName = null);

    /// <summary>
    /// Maps the form value with the specified name to a value of the specified type.
    /// <param name="context">The <see cref="FormValueMappingContext"/>.</param>
    /// </summary>
    void Map(FormValueMappingContext context);
}
