// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A context for <see cref="IValueProviderFactory"/>.
/// </summary>
public class ValueProviderFactoryContext
{
    /// <summary>
    /// Creates a new <see cref="ValueProviderFactoryContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    public ValueProviderFactoryContext(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ActionContext = context;
    }

    /// <summary>
    /// Gets the <see cref="ActionContext"/> associated with this context.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Gets the list of <see cref="IValueProvider"/> instances.
    /// <see cref="IValueProviderFactory"/> instances should add the appropriate
    /// <see cref="IValueProvider"/> instances to this list.
    /// </summary>
    public IList<IValueProvider> ValueProviders { get; } = new List<IValueProvider>();
}
