// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlLocalizer"/> that provides localized HTML content.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="System.Type"/> to scope the resource names.</typeparam>
    public interface IHtmlLocalizer<TResource> : IHtmlLocalizer
    {
    }
}