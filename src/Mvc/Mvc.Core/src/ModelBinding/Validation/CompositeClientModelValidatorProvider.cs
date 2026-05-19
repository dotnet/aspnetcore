// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Aggregate of <see cref="IClientModelValidatorProvider"/>s that delegates to its underlying providers.
/// </summary>
public class CompositeClientModelValidatorProvider : IClientModelValidatorProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompositeClientModelValidatorProvider"/>.
    /// </summary>
    /// <param name="providers">
    /// A collection of <see cref="IClientModelValidatorProvider"/> instances.
    /// </param>
    public CompositeClientModelValidatorProvider(IEnumerable<IClientModelValidatorProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        ValidatorProviders = new List<IClientModelValidatorProvider>(providers);
    }

    /// <summary>
    /// Gets a list of <see cref="IClientModelValidatorProvider"/> instances.
    /// </summary>
    public IReadOnlyList<IClientModelValidatorProvider> ValidatorProviders { get; }

    /// <inheritdoc />
    public void CreateValidators(ClientValidatorProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Perf: Avoid allocations
        for (var i = 0; i < ValidatorProviders.Count; i++)
        {
            ValidatorProviders[i].CreateValidators(context);
        }
    }
}
