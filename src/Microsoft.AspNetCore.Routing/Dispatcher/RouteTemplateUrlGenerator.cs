// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    // This isn't a proposed design, just a placeholder to demonstrate that things are wired up correctly.
    public class RouteTemplateUrlGenerator
    {
        private readonly TemplateAddressSelector _addressSelector;
        private readonly ObjectPool<UriBuildingContext> _pool;
        private readonly UrlEncoder _urlEncoder;

        public RouteTemplateUrlGenerator(TemplateAddressSelector addressSelector, UrlEncoder urlEncoder, ObjectPool<UriBuildingContext> pool)
        {
            _addressSelector = addressSelector;
            _urlEncoder = urlEncoder;
            _pool = pool;
        }

        public string GenerateUrl(HttpContext httpContext, object values)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var address = _addressSelector.SelectAddress(new DispatcherValueCollection(values)) as ITemplateAddress;
            if (address == null)
            {
                throw new InvalidOperationException("Can't find address");
            }

            var binder = new TemplateBinder(_urlEncoder, _pool, Template.TemplateParser.Parse(address.Template), new RouteValueDictionary());

            var feature = httpContext.Features.Get<IDispatcherFeature>();
            var result = binder.GetValues(feature.Values.AsRouteValueDictionary(), new RouteValueDictionary(values));
            if (result == null)
            {
                return null;
            }

            return binder.BindValues(result.AcceptedValues);
        }
    }
}
