// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Contains extension methods for fetching encoders from an <see cref="IServiceProvider"/>.
    /// </summary>
    public static class EncoderServiceProviderExtensions
    {
        /// <summary>
        /// Retrieves an <see cref="IHtmlEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static IHtmlEncoder GetHtmlEncoder(this IServiceProvider serviceProvider)
        {
            return (IHtmlEncoder)serviceProvider?.GetService(typeof(IHtmlEncoder)) ?? HtmlEncoder.Default;
        }

        /// <summary>
        /// Retrieves an <see cref="IJavaScriptStringEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static IJavaScriptStringEncoder GetJavaScriptStringEncoder(this IServiceProvider serviceProvider)
        {
            return (IJavaScriptStringEncoder)serviceProvider?.GetService(typeof(IJavaScriptStringEncoder)) ?? JavaScriptStringEncoder.Default;
        }

        /// <summary>
        /// Retrieves an <see cref="IUrlEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static IUrlEncoder GetUrlEncoder(this IServiceProvider serviceProvider)
        {
            return (IUrlEncoder)serviceProvider?.GetService(typeof(IUrlEncoder)) ?? UrlEncoder.Default;
        }
    }
}
