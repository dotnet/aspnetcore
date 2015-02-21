// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectResult : ActionResult
    {
        private string _url;

        public RedirectResult([NotNull] string url)
            : this(url, permanent: false)
        {
        }

        public RedirectResult([NotNull] string url, bool permanent)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            Permanent = permanent;
            Url = url;
        }

        public bool Permanent { get; set; }

        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "value");
                }

                _url = value;
            }
        }

        public IUrlHelper UrlHelper { get; set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var urlHelper = GetUrlHelper(context);

            // IsLocalUrl is called to handle  Urls starting with '~/'.
            var destinationUrl = Url;
            if (urlHelper.IsLocalUrl(destinationUrl))
            {
                destinationUrl = urlHelper.Content(Url);
            }

            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            return UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
        }
    }
}