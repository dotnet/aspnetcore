// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Provides a hint of the <see cref="ITagHelper"/>'s output element.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OutputElementHintAttribute : Attribute
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="OutputElementHintAttribute"/> class.
    /// </summary>
    /// <param name="outputElement">
    /// The HTML element the <see cref="ITagHelper"/> may output.
    /// </param>
    public OutputElementHintAttribute(string outputElement)
    {
        ArgumentNullException.ThrowIfNull(outputElement);

        OutputElement = outputElement;
    }

    /// <summary>
    /// The HTML element the <see cref="ITagHelper"/> may output.
    /// </summary>
    public string OutputElement { get; }
}
