// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

/// <summary>
/// An error that occurred during the form mapping process.
/// </summary>
public sealed class FormMappingError
{
    private static readonly char[] Separators = new char[] { '.', '[' };
    private readonly List<FormattableString> _errorMessages;

    /// <summary>
    /// Initializes a new instance of <see cref="FormMappingError"/>.
    /// </summary>
    /// <param name="path">The path from the root of the mapping operation to the property or element that failed to map.</param>
    /// <param name="errorMessages">The error messages associated with the mapping error.</param>
    /// <param name="attemptedValue">The attempted value that failed to map.</param>
    internal FormMappingError(string path, List<FormattableString> errorMessages, string? attemptedValue)
    {
        _errorMessages = errorMessages;
        AttemptedValue = attemptedValue;
        Path = path;
        Name = GetName(Path);
    }

    /// <summary>
    /// Gets or sets the instance that contains the property or element that failed to map.
    /// </summary>
    /// <remarks>
    /// For object models, this is the instance of the object that contains the property that failed to map.
    /// For collection models, this is the collection instance that contains the element that failed to map.
    /// For dictionaries, this is the dictionary instance that contains the element that failed to map.
    /// </remarks>
    public object Container { get; internal set; } = null!;

    /// <summary>
    /// Gets or sets the name of the property or element that failed to map.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the full path from the model root to the property or element that failed to map.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the list of error messages associated with the mapping errors for this field.
    /// </summary>
    public IReadOnlyList<FormattableString> ErrorMessages => _errorMessages;

    /// <summary>
    /// Gets the attempted value that failed to map (if any).
    /// </summary>
    public string? AttemptedValue { get; }

    private static string GetName(string path)
    {
        var errorKey = path;
        var lastSeparatorIndex = path.LastIndexOfAny(Separators);
        if (lastSeparatorIndex >= 0)
        {
            if (path[lastSeparatorIndex] == '[')
            {
                var closingBracket = path.IndexOf(']', lastSeparatorIndex);
                // content within brackets
                errorKey = path[(lastSeparatorIndex + 1)..closingBracket];
            }
            else
            {
                errorKey = path[(lastSeparatorIndex + 1)..];
            }
        }

        return errorKey;
    }

    internal void AddError(FormattableString error)
    {
        _errorMessages.Add(error);
    }
}
