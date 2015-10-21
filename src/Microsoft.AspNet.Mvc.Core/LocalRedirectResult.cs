// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a redirect to the supplied local URL.
    /// </summary>
    public class LocalRedirectResult : ActionResult
    {
        private string _localUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        public LocalRedirectResult(string localUrl)
             : this(localUrl, permanent: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="url">The local URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        public LocalRedirectResult(string localUrl, bool permanent)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(localUrl));
            }

            Permanent = permanent;
            Url = localUrl;
        }

        /// <summary>
        /// Gets or sets the value that specifies that the redirect should be permanent if true or temporary if false.
        /// </summary>
        public bool Permanent { get; set; }

        /// <summary>
        /// Gets or sets the local URL to redirect to.
        /// </summary>
        public string Url
        {
            get
            {
                return _localUrl;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                _localUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper"/> for this result.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <inheritdoc />
        public override void ExecuteResult(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<LocalRedirectResult>();

            var urlHelper = GetUrlHelper(context);

            if (!urlHelper.IsLocalUrl(Url))
            {
                throw new InvalidOperationException(Resources.UrlNotLocal);
            }

            var destinationUrl = urlHelper.Content(Url);
            logger.LocalRedirectResultExecuting(destinationUrl);
            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            return UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
        }
    }
}
