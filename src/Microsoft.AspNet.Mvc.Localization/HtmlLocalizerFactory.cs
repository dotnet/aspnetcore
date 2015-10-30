// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlLocalizerFactory"/> that creates instances of <see cref="HtmlLocalizer"/>.
    /// </summary>
    public class HtmlLocalizerFactory : IHtmlLocalizerFactory
    {
        private readonly IStringLocalizerFactory _factory;
        private readonly HtmlEncoder _encoder;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizerFactory">The <see cref="IStringLocalizerFactory"/>.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/>.</param>
        public HtmlLocalizerFactory(IStringLocalizerFactory localizerFactory, HtmlEncoder encoder)
        {
            if (localizerFactory == null)
            {
                throw new ArgumentNullException(nameof(localizerFactory));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            _factory = localizerFactory;
            _encoder = encoder;
        }

        /// <summary>
        /// Creates an <see cref="HtmlLocalizer"/> using the <see cref="System.Reflection.Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="HtmlLocalizer"/>.</returns>
        public virtual IHtmlLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            return new HtmlLocalizer(_factory.Create(resourceSource), _encoder);
        }

        /// <summary>
        /// Creates an <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="HtmlLocalizer"/>.</returns>
        public virtual IHtmlLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var localizer = _factory.Create(baseName, location);
            return new HtmlLocalizer(localizer, _encoder);
        }
    }
}