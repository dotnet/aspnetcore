// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Allows customization of the <see cref="PageApplicationModel"/>.
/// </summary>
public interface IPageApplicationModelConvention : IPageConvention
{
    /// <summary>
    /// Called to apply the convention to the <see cref="PageApplicationModel"/>.
    /// </summary>
    /// <param name="model">The <see cref="PageApplicationModel"/>.</param>
    void Apply(PageApplicationModel model);
}
