// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Aggregate of <see cref="IModelValidatorProvider"/>s that delegates to its underlying providers.
/// </summary>
public class CompositeModelValidatorProvider : IModelValidatorProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompositeModelValidatorProvider"/>.
    /// </summary>
    /// <param name="providers">
    /// A collection of <see cref="IModelValidatorProvider"/> instances.
    /// </param>
    public CompositeModelValidatorProvider(IList<IModelValidatorProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        ValidatorProviders = providers;
    }

    /// <summary>
    /// Gets the list of <see cref="IModelValidatorProvider"/> instances.
    /// </summary>
    public IList<IModelValidatorProvider> ValidatorProviders { get; }

    /// <inheritdoc />
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        // Perf: Avoid allocations
        for (var i = 0; i < ValidatorProviders.Count; i++)
        {
            ValidatorProviders[i].CreateValidators(context);
        }
    }
}
