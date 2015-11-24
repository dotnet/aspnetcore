// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Extensions.OptionsModel;

namespace UrlHelperSample.Web
{
    public class CustomUrlHelperFactory : IUrlHelperFactory
    {
        private readonly AppOptions _options;

        public CustomUrlHelperFactory(IOptions<AppOptions> options)
        {
            _options = options.Value;
        }

        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            return new CustomUrlHelper(context, _options);
        }
    }
}
