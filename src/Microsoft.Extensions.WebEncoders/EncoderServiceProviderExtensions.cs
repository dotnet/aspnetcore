// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Contains extension methods for fetching encoders from an <see cref="IServiceProvider"/>.
    /// </summary>
    public static class EncoderServiceProviderExtensions
    {
        /// <summary>
        /// Retrieves an <see cref="HtmlEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static HtmlEncoder GetHtmlEncoder(this IServiceProvider serviceProvider)
        {
            return (HtmlEncoder)serviceProvider?.GetService(typeof(HtmlEncoder)) ?? HtmlEncoder.Default;
        }

        /// <summary>
        /// Retrieves an <see cref="JavaScriptEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static JavaScriptEncoder GetJavaScriptEncoder(this IServiceProvider serviceProvider)
        {
            return (JavaScriptEncoder)serviceProvider?.GetService(typeof(JavaScriptEncoder)) ?? JavaScriptEncoder.Default;
        }

        /// <summary>
        /// Retrieves an <see cref="UrlEncoder"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method is guaranteed never to return null.
        /// It will return a default encoder instance if <paramref name="serviceProvider"/> does not contain one or is null.
        /// </remarks>
        public static UrlEncoder GetUrlEncoder(this IServiceProvider serviceProvider)
        {
            return (UrlEncoder)serviceProvider?.GetService(typeof(UrlEncoder)) ?? UrlEncoder.Default;
        }
    }
}
