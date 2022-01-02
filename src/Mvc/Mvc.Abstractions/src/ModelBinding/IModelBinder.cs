// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Defines an interface for model binders.
/// </summary>
public interface IModelBinder
{
    /// <summary>
    /// Attempts to bind a model.
    /// </summary>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Task"/> which will complete when the model binding process completes.
    /// </para>
    /// <para>
    /// If model binding was successful, the <see cref="ModelBindingContext.Result"/> should have
    /// <see cref="ModelBindingResult.IsModelSet"/> set to <c>true</c>.
    /// </para>
    /// <para>
    /// A model binder that completes successfully should set <see cref="ModelBindingContext.Result"/> to
    /// a value returned from <see cref="ModelBindingResult.Success"/>.
    /// </para>
    /// </returns>
    Task BindModelAsync(ModelBindingContext bindingContext);
}
