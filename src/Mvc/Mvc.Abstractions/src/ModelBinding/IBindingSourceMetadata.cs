// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Metadata which specifies the data source for model binding.
/// </summary>
public interface IBindingSourceMetadata
{
    /// <summary>
    /// Gets the <see cref="BindingSource"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="BindingSource"/> is metadata which can be used to determine which data
    /// sources are valid for model binding of a property or parameter.
    /// </remarks>
    BindingSource? BindingSource { get; }
}
