// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Result of <see cref="IRazorPageFactoryProvider.CreateFactory(string)"/>.
    /// </summary>
    public struct RazorPageFactoryResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorPageFactoryResult"/> with the
        /// specified <paramref name="expirationTokens"/>.
        /// </summary>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances.</param>
        public RazorPageFactoryResult(IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            ExpirationTokens = expirationTokens;
            RazorPageFactory = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RazorPageFactoryResult"/> with the
        /// specified <see cref="IRazorPage"/> factory.
        /// </summary>
        /// <param name="razorPageFactory">The <see cref="IRazorPage"/> factory.</param>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances.</param>
        public RazorPageFactoryResult(
            Func<IRazorPage> razorPageFactory,
            IList<IChangeToken> expirationTokens)
        {
            if (razorPageFactory == null)
            {
                throw new ArgumentNullException(nameof(razorPageFactory));
            }

            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            RazorPageFactory = razorPageFactory;
            ExpirationTokens = expirationTokens;
        }

        /// <summary>
        /// The <see cref="IRazorPage"/> factory.
        /// </summary>
        /// <remarks>This property is <c>null</c> when <see cref="Success"/> is <c>false</c>.</remarks>
        public Func<IRazorPage> RazorPageFactory { get; }

        /// <summary>
        /// One or more <see cref="IChangeToken"/>s associated with this instance of
        /// <see cref="RazorPageFactoryResult"/>.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens { get; }

        /// <summary>
        /// Gets a value that determines if the page was successfully located.
        /// </summary>
        public bool Success => RazorPageFactory != null;
    }
}
