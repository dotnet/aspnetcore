// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RoutePatternTemplate : Template
    {
        private readonly RoutePatternBinder _binder;

        public RoutePatternTemplate(RoutePatternBinder binder)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            _binder = binder;
        }

        public override string GetUrl(DispatcherValueCollection values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return GetUrl(null, values);
        }

        public override string GetUrl(HttpContext httpContext, DispatcherValueCollection values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var ambientValues = GetAmbientValues(httpContext);
            var result = _binder.GetValues(ambientValues, values);
            if (result.acceptedValues == null)
            {
                return null;
            }

            return _binder.BindValues(result.acceptedValues);
        }

        private DispatcherValueCollection GetAmbientValues(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return new DispatcherValueCollection();
            }

            var feature = httpContext.Features.Get<IDispatcherFeature>();
            if (feature == null)
            {
                return new DispatcherValueCollection();
            }

            return feature.Values;
        }
    }
}
