// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// This API supports the ASP.NET Core infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
[Obsolete]
public interface IStartupConfigureContainerFilter<TContainerBuilder>
{
    /// <summary>
    /// Extends the provided <paramref name="container"/> and returns a modified <see cref="Action"/> action of the same type.
    /// </summary>
    /// <param name="container">The ConfigureContainer method to extend.</param>
    /// <returns>A modified <see cref="Action"/>.</returns>
    Action<TContainerBuilder> ConfigureContainer(Action<TContainerBuilder> container);
}
