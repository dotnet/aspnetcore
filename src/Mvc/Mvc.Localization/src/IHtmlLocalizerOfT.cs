// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// An <see cref="IHtmlLocalizer"/> that provides localized HTML content.
/// </summary>
/// <typeparam name="TResource">The <see cref="System.Type"/> to scope the resource names.</typeparam>
public interface IHtmlLocalizer<TResource> : IHtmlLocalizer
{
}
