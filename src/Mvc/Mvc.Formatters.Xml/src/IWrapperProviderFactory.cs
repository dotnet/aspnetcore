// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Create a <see cref="IWrapperProvider"/> given a <see cref="WrapperProviderContext"/>.
/// </summary>
public interface IWrapperProviderFactory
{
    /// <summary>
    /// Gets the <see cref="IWrapperProvider"/> for the provided context.
    /// </summary>
    /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
    /// <returns>A wrapping provider if the factory decides to wrap the type, else <c>null</c>.</returns>
    IWrapperProvider? GetProvider(WrapperProviderContext context);
}
