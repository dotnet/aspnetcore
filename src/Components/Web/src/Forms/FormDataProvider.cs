// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the state of the form being processed.
/// </summary>
public abstract class FormDataProvider
{
    /// <summary>
    /// Gets the name of the form associated with the data in this form.
    /// </summary>
    public abstract string? Name { get; }

    /// <summary>
    /// Indicates whether there is form data available.
    /// </summary>
    public bool IsFormDataAvailable => Name != null;

    /// <summary>
    /// Gets the entries associated with the current form data.
    /// </summary>
    public abstract IReadOnlyDictionary<string, StringValues> Entries { get; }
}
