// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="TBinder">A <see cref="Type"/> which implements <see cref="IModelBinder"/>.</typeparam>
/// <remarks>
/// This is a derived generic variant of the <see cref="ModelBinderAttribute"/>.
/// Ensure that only one instance of either attribute is provided on the target.
/// </remarks>
public class ModelBinderAttribute<TBinder> : ModelBinderAttribute where TBinder : IModelBinder
{
    /// <summary>
    /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Subclass this attribute and set <see cref="BindingSource"/> if <see cref="BindingSource.Custom"/> is not
    /// correct for the specified type parameter.
    /// </remarks>
    public ModelBinderAttribute() : base(typeof(TBinder)) { }
}
