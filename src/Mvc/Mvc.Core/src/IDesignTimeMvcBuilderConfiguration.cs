// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Configures the <see cref="IMvcBuilder"/>. Implement this interface to enable design-time configuration
/// (for instance during pre-compilation of views) of <see cref="IMvcBuilder"/>.
/// </summary>
public interface IDesignTimeMvcBuilderConfiguration
{
    /// <summary>
    /// Configures the <see cref="IMvcBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    void ConfigureMvc(IMvcBuilder builder);
}
