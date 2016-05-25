// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
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
        /// <param name="localUrl">The local URL to redirect to.</param>
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

            var executor = context.HttpContext.RequestServices.GetRequiredService<LocalRedirectResultExecutor>();
            executor.Execute(context, this);
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            var urlHelper = UrlHelper;
            if (urlHelper == null)
            {
                var services = context.HttpContext.RequestServices;
                urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(context);
            }

            return urlHelper;
        }
    }
}
