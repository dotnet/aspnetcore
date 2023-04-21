// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// An interface implemented by <see cref="FormDataProvider"/> classes that can receive
/// the form data from the host environment.
/// </summary>
public interface IHostEnvironmentFormDataProvider
{
    /// <summary>
    /// Sets the form data from the environment.
    /// </summary>
    /// <param name="form">The form data</param>
    /// <param name="name">The form name</param>
    public void SetFormData(string name, IReadOnlyDictionary<string, string?> form);
}
