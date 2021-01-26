// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a Found (302), Moved Permanently (301), Temporary Redirect (307),
    /// or Permanent Redirect (308) response with a Location header to the supplied URL.
    /// </summary>
    public class RedirectResult : ActionResult, IKeepTempDataResult
    {
        private string _url;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="url">The local URL to redirect to.</param>
        public RedirectResult(string url)
            : this(url, permanent: false)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        public RedirectResult(string url, bool permanent)
            : this(url, permanent, preserveMethod: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
        public RedirectResult(string url, bool permanent, bool preserveMethod)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            Permanent = permanent;
            PreserveMethod = preserveMethod;
            Url = url;
        }

        /// <summary>
        /// Gets or sets the value that specifies that the redirect should be permanent if true or temporary if false.
        /// </summary>
        public bool Permanent { get; set; }

        /// <summary>
        /// Gets or sets an indication that the redirect preserves the initial request method.
        /// </summary>
        public bool PreserveMethod { get; set; }

        /// <summary>
        /// Gets or sets the URL to redirect to.
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                _url = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper"/> for this result.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<RedirectResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}