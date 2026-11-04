// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A collection of <see cref="ModelError"/> instances.
/// </summary>
public class ModelErrorCollection : Collection<ModelError>
{
    /// <summary>
    /// Adds the specified <paramref name="exception"/> instance.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/></param>
    public void Add(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Add(new ModelError(exception));
    }

    /// <summary>
    /// Adds the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public void Add(string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        Add(new ModelError(errorMessage));
    }
}
